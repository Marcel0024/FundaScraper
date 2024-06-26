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
    ILoggerFactory loggerFactory,
    IOptions<FundaScraperSettings> appSettings,
    IHttpClientFactory httpClientFactory,
    WebhookSink webhookSink,
    CronPeriodicTimer periodicTimer,
    TimeProvider timeProvider) : BackgroundService
{
    private readonly FundaScraperSettings settings = appSettings.Value;
    private readonly ILogger logger = loggerFactory.CreateLogger(typeof(FundaScraperBackgroundService).FullName!);
    private readonly bool IsErrorWebhookEnabled = !string.IsNullOrWhiteSpace(appSettings.Value.ErrorWebHookUrl);

    private ScraperEngine ScraperEngine { get; set; } = default!;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var urlsToScrape = GetFundaUrlPaginatedPages(settings.FundaUrl, settings.StartPage, settings.TotalPages);

        ScraperEngine = await new ScraperEngineBuilder()
            .GetWithBrowser(urlsToScrape, actions => actions
                .ScrollToEnd()
                .Build())
            .PaginateWithBrowser(settings.ListingLinkSelector, settings.PaginationSelector, actions => actions
                .Wait(milliseconds: settings.WaitOnScrapingPageMilliseconds)
                .ScrollToEnd()
                .Build())
            .HeadlessMode(false)
            .WithLogger(loggerFactory.CreateLogger(nameof(ScraperEngine)))
            .Parse(
                [
                    new(nameof(ListingModel.Title), settings.TitleSelector),
                    new(nameof(ListingModel.ZipCode), settings.ZipCodeSelector),
                    new(nameof(ListingModel.Price), settings.PriceSelector),
                    new(nameof(ListingModel.Area), settings.AreaSelector),
                    new(nameof(ListingModel.TotalRooms), settings.TotalRoomsSelector)
                ])
            .AddSink(webhookSink)
            .AddSink(new CsvFileSink(Constants.FileNames.ResultsFilePath, dataCleanupOnStart: true))
            .IgnoreUrls([.. settings.IgnoreUrls])
            .WithParallelismDegree(settings.ParallelismDegree)
            .PageCrawlLimit(settings.PageCrawlLimit)
            .BuildAsync();

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (settings.RunOnStartup)
        {
            await Scrape(ScraperEngine, cancellationToken);
        }

        while (await periodicTimer.WaitForNextTickAsync(cancellationToken))
        {
            await Scrape(ScraperEngine, cancellationToken);
        }
    }

    private async Task Scrape(ScraperEngine scraperEngine, CancellationToken cancellationToken)
    {
        logger.LogInformation("{datetimenow} Starting scraping run.", timeProvider.GetLocalNow());

        // The engine doesn't kill it's self after scraping everything, so we time box it.
        using var timeboxCts = new CancellationTokenSource(TimeSpan.FromMinutes(settings.ScraperEngineTimeBoxInMinutes));
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

        logger.LogInformation("{datetimenow} Finished scraping run. Waiting for next tick.", timeProvider.GetLocalNow());
    }

    private async Task NotifyErrorWebHook(string errorMessage, CancellationToken cancellationToken)
    {
        try
        {
            using var http = httpClientFactory.CreateClient();
            await http.PostAsJsonAsync(settings.ErrorWebHookUrl, new
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
            var uri = HttpUtility.ParseQueryString(fundaUrl);

            uri["search_result"] = i.ToString();

            return uri!.ToString()!;
        }).ToArray();
    }
}
