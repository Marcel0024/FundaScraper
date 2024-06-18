using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;

namespace FundaScraper.App;

internal class AppSettings
{
    [Required, Url]
    [ConfigurationKeyName("FUNDA_URL")]
    public required string FundaUrl { get; init; }

    [Required, Url]
    [ConfigurationKeyName("WEBHOOK_URL")]
    public required string WebHookUrl { get; init; }

    [Url]
    [ConfigurationKeyName("ERROR_WEBHOOK_URL")]
    public string? ErrorWebHookUrl { get; init; }

    [Range(10, 60 * 60 * 24 * 7)]
    [ConfigurationKeyName("INTERVAL_IN_MINUTES")]
    public int IntervalInMinutes { get; init; }

    [Range(5, 1000)]
    [ConfigurationKeyName("PAGE_CRAWL_LIMIT")]
    public int PageCrawlLimit { get; init; }

    [ConfigurationKeyName("RUN_ON_STARTUP")]
    public bool RunOnStartup { get; init; }
}