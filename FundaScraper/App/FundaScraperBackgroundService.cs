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
    IHostEnvironment environment,
    ILogger<FundaScraperBackgroundService> logger,
    IOptions<FundaScraperSettings> settings,
    IHttpClientFactory httpClientFactory,
    WebhookSink webhookSink,
    CronPeriodicTimer periodicTimer) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var scraperEngineBuilder = new ScraperEngineBuilder()
            .GetWithBrowser(GetFundaURLPages(settings.Value.FundaUrl, settings.Value.StartPage, settings.Value.TotalPages), actions => actions
                .ScrollToEnd()
                .Build())
            .PaginateWithBrowser(settings.Value.ListingLinkSelector, ".dont-exist", actions => actions
                .Wait(milliseconds: 3000)
                .ScrollToEnd()
                .Build())
            .HeadlessMode(false)
            .Parse(
                [
                    new(nameof(ListingModel.Title), settings.Value.TitleSelector),
                    new(nameof(ListingModel.Price), settings.Value.PriceSelector),
                    new(nameof(ListingModel.Area), settings.Value.AreaSelector),
                    new(nameof(ListingModel.TotalRooms), settings.Value.TotalRoomsSelector),
                    new(nameof(ListingModel.ZipCode), settings.Value.ZipCodeSelector)
                ])
            .AddSink(webhookSink)
            .AddSink(new CsvFileSink(Path.Combine("/data", "results.csv"), dataCleanupOnStart: true))
            .WithParallelismDegree(settings.Value.ParallelismDegree)
            .PageCrawlLimit(settings.Value.PageCrawlLimit);

        if (environment.IsDevelopment())
        {
            scraperEngineBuilder
                .AddSink(new ConsoleSink())
                .LogToConsole();
        }

        var scraperEngine = await scraperEngineBuilder.BuildAsync();

        if (settings.Value.RunOnStartup)
        {
            await Scrape(scraperEngine);
        }

        while (await periodicTimer.WaitForNextTickAsync(stoppingToken))
        {
            await Scrape(scraperEngine);
        }
    }

    private async Task Scrape(ScraperEngine scraperEngine)
    {
        logger.LogInformation("Starting scraping run.");

        var cts = new CancellationTokenSource();

        // The engine doesn't kill it's self after scraping everything, so we time box it.
        cts.CancelAfter(TimeSpan.FromMinutes(10));

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
            if (settings.Value.ErrorWebHookUrl is not null)
            {
                try
                {
                    using var http = httpClientFactory.CreateClient();
                    await http.PostAsJsonAsync(settings.Value.ErrorWebHookUrl, new
                    {
                        Message = $"{ex.Message}. Funda probably changed their site."
                    }, CancellationToken.None);
                }
                catch { } // Error on error let's ignore
            }

            throw;
        }

        logger.LogInformation("Finished scraping run. Waiting for next interval.");
    }

    private static string[] GetFundaURLPages(string fundaUrl, int startPage, int totalPages)
    {
        return Enumerable.Range(startPage, totalPages).Select(i =>
        {
            var query = HttpUtility.ParseQueryString(fundaUrl);

            query["search_result"] = i.ToString();

            return query!.ToString()!;
        }).ToArray();
    }
}
