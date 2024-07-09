using CocoCrawler;
using CocoCrawler.Builders;
using FundaScraper.Models;
using FundaScraper.Utilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Web;

namespace FundaScraper.App;

internal class FundaScraperBackgroundService(
    ILoggerFactory loggerFactory,
    IOptions<FundaScraperSettings> appSettings,
    IHttpClientFactory httpClientFactory,
    WebhookCrawlOutput webhookCrawlOutput,
    CronPeriodicTimer periodicTimer,
    TimeProvider timeProvider) : BackgroundService
{
    private readonly FundaScraperSettings settings = appSettings.Value;
    private readonly ILogger logger = loggerFactory.CreateLogger<FundaScraperBackgroundService>();

    private CrawlerEngine ScraperEngine { get; set; } = default!;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var urlsToScrape = GetFundaUrlPaginatedPages(settings.FundaUrl, settings.StartPage, settings.TotalPages);

        ScraperEngine = await new CrawlerEngineBuilder()
            .AddPages(urlsToScrape, (page) => page
                .ConfigurePageActions(options => options.ScrollToEnd().Wait(settings.WaitOnScrapingPageMilliseconds))
                .ExtractList(settings.ListingContainersSelector, [
                    new(nameof(ListingModel.Url), settings.UrlSelector, "href"),
                    new(nameof(ListingModel.Title), settings.TitleSelector),
                    new(nameof(ListingModel.ZipCode), settings.ZipCodeSelector),
                    new(nameof(ListingModel.Price), settings.PriceSelector),
                    new(nameof(ListingModel.Area), settings.AreaSelector),
                    new(nameof(ListingModel.TotalRooms), settings.TotalRoomsSelector)
                ])
                .AddPagination(settings.PaginationSelector)
                .AddOutputToCsvFile(Constants.FileNames.ResultsFilePath, cleanOnStartup: true)
                .AddOutput(webhookCrawlOutput)
            )
            .ConfigureEngine(options => options
                .UseHeadlessMode(false)
                .WithParallelismDegree(settings.ParallelismDegree)
                .WithIgnoreUrls([.. settings.IgnoreUrls])
                .WithLoggerFactory(loggerFactory)
                .TotalPagesToCrawl(settings.PageCrawlLimit)
            )
            .BuildAsync(cancellationToken);

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

    private async Task Scrape(CrawlerEngine scraperEngine, CancellationToken cancellationToken)
    {
        logger.LogInformation("{datetimenow} Starting scraping run.", timeProvider.GetLocalNow());

        try
        {
            await scraperEngine.RunAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            if (settings.IsErrorWebhookEnabled)
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
