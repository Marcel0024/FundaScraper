using FundaScraper.Models;
using FundaScraper.Utilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Web;
using WebReaper.Builders;
using WebReaper.Core;
using WebReaper.Sinks.Concrete;

namespace FundaScraper.App;

internal class FundaScraperBackgroundService(
    ILogger<FundaScraperBackgroundService> logger,
    IOptions<FundaScraperSettings> settings,
    IHttpClientFactory httpClientFactory,
    WebhookSink webhookSink,
    CronPeriodicTimer periodicTimer) : BackgroundService
{
    private readonly bool IsErrorWebhookEnabled = !string.IsNullOrWhiteSpace(settings.Value.ErrorWebHookUrl);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var urlsToScrape = GetFundaUrlPaginatedPages(settings.Value.FundaUrl, settings.Value.StartPage, settings.Value.TotalPages);

        var scraperEngine = await new ScraperEngineBuilder()
            .GetWithBrowser(urlsToScrape, actions => actions
                .ScrollToEnd()
                .Build())
            .PaginateWithBrowser(settings.Value.ListingLinkSelector, ".dont-exist", actions => actions
                .Wait(milliseconds: settings.Value.WaitOnScrapingPageMilliseconds)
                .ScrollToEnd()
                .Build())
            .HeadlessMode(false)
            .WithLogger(logger)
            .Parse(
                [
                    new(nameof(ListingModel.Title), settings.Value.TitleSelector),
                    new(nameof(ListingModel.ZipCode), settings.Value.ZipCodeSelector),
                    new(nameof(ListingModel.Price), settings.Value.PriceSelector),
                    new(nameof(ListingModel.Area), settings.Value.AreaSelector),
                    new(nameof(ListingModel.TotalRooms), settings.Value.TotalRoomsSelector)
                ])
            .AddSink(webhookSink)
            .AddSink(new CsvFileSink(Constants.FileNames.ResultsFilePath, dataCleanupOnStart: true))
            .WithParallelismDegree(settings.Value.ParallelismDegree)
            .PageCrawlLimit(settings.Value.PageCrawlLimit)
            .BuildAsync();

        if (settings.Value.RunOnStartup)
        {
            await Scrape(scraperEngine, cancellationToken);
        }

        while (await periodicTimer.WaitForNextTickAsync(cancellationToken))
        {
            await Scrape(scraperEngine, cancellationToken);
        }
    }

    private async Task Scrape(ScraperEngine scraperEngine, CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting scraping run.");

        // The engine doesn't kill it's self after scraping everything, so we time box it.
        using var timeboxCts = new CancellationTokenSource(TimeSpan.FromMinutes(settings.Value.ScraperEngineTimeBoxInMinutes));
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeboxCts.Token);

        try
        {
            await scraperEngine.RunAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Timeboxed - ignore
        }
        catch (Exception ex)
        {
            if (IsErrorWebhookEnabled)
            {
                await NotifyErrorWebHook(ex.Message, cancellationToken);
            }

            throw;
        }

        logger.LogInformation("Finished scraping run. Waiting for next tick.");
    }

    private async Task NotifyErrorWebHook(string errorMessage, CancellationToken cancellationToken)
    {
        try
        {
            using var http = httpClientFactory.CreateClient();
            await http.PostAsJsonAsync(settings.Value.ErrorWebHookUrl, new
            {
                message = $"{errorMessage}. Funda probably changed their site."
            }, cancellationToken);
        }
        catch (Exception webhookEx)
        {
            logger.LogError(webhookEx, "Failed to send error webhook.");
        }
    }

    private static string[] GetFundaUrlPaginatedPages(string fundaUrl, int startPage, int totalPages)
    {
        return Enumerable.Range(startPage, totalPages).Select(i =>
        {
            var urlWithQueryPath = HttpUtility.ParseQueryString(fundaUrl);

            urlWithQueryPath["search_result"] = i.ToString();

            return urlWithQueryPath!.ToString()!;
        }).ToArray();
    }
}
