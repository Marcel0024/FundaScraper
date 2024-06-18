using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using WebReaper.Builders;
using WebReaper.Core;

namespace FundaScraper.App;

internal class FundaScraperBackgroundService(
    ILogger<FundaScraperBackgroundService> logger,
    IOptions<AppSettings> appSettings,
    IHttpClientFactory httpClientFactory,
    WebhookSink webhookSink) : BackgroundService
{
    private readonly PeriodicTimer PeriodicTimer = new(TimeSpan.FromMinutes(appSettings.Value.IntervalInMinutes));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var scraperEngine = await new ScraperEngineBuilder()
            .GetWithBrowser([appSettings.Value.FundaUrl], actions => actions
                .Wait(milliseconds: 1000)
                .ScrollToEnd()
                .Build())
            .FollowWithBrowser("a.text-secondary-70.cursor-pointer", actions => actions
                .Wait(milliseconds: 1000)
                .ScrollToEnd()
                .Build())
            .HeadlessMode(false)
            .Parse(
                [
                    new("Name", "span.block"),
                    new("Price", "div.flex.gap-2.font-bold")
                ])
            .AddSink(webhookSink)
            .PageCrawlLimit(appSettings.Value.PageCrawlLimit)
            .BuildAsync();

        if (appSettings.Value.RunOnStartup)
        {
            await Scrape(scraperEngine);
        }

        while (await PeriodicTimer.WaitForNextTickAsync(stoppingToken))
        {
            await Scrape(scraperEngine);
        }
    }

    private async Task Scrape(ScraperEngine scraperEngine)
    {
        logger.LogInformation("Starting scraping run.");

        var cts = new CancellationTokenSource();

        // The engine doesn't kill it's self after scraping everything, so we time box it.
        cts.CancelAfter(TimeSpan.FromMinutes(5));

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
            if (appSettings.Value.ErrorWebHookUrl is not null)
            {
                try
                {
                    using var http = httpClientFactory.CreateClient();
                    await http.PostAsJsonAsync(appSettings.Value.ErrorWebHookUrl, new
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
}
