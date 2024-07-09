using Cronos;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;

namespace FundaScraper;

internal class FundaScraperSettings
{
    /// <summary>
    /// CSS SELECTORS
    /// </summary>
    [Required]
    [ConfigurationKeyName("LISTING_CONTAINERS_SELECTOR")]
    public required string ListingContainersSelector { get; init; }
    [Required]
    [ConfigurationKeyName("URL_SELECTOR")]
    public required string UrlSelector { get; init; }
    [Required]
    [ConfigurationKeyName("PAGINATION_SELECTOR")]
    public required string PaginationSelector { get; init; }
    [Required]
    [ConfigurationKeyName("TITLE_SELECTOR")]
    public required string TitleSelector { get; init; }
    [Required]
    [ConfigurationKeyName("ZIP_CODE_SELECTOR")]
    public required string ZipCodeSelector { get; init; }
    [Required]
    [ConfigurationKeyName("PRICE_SELECTOR")]
    public required string PriceSelector { get; init; }
    [Required]
    [ConfigurationKeyName("AREA_SELECTOR")]
    public required string AreaSelector { get; init; }
    [Required]
    [ConfigurationKeyName("TOTAL_ROOMS_SELECTOR")]
    public required string TotalRoomsSelector { get; init; }

    /// <summary>
    /// ENGINE SETTINGS
    /// </summary>
    [Required, CronExpression]
    [ConfigurationKeyName("CRON")]
    public required string CronExpression { get; init; }

    [Required, Url(startsWith: "https://www.", isRequired: true)]
    [ConfigurationKeyName("FUNDA_URL")]
    public required string FundaUrl { get; init; }

    [Url(startsWith: "http", isRequired: false)]
    [ConfigurationKeyName("WEBHOOK_URL")]
    public string? WebHookUrl { get; init; }

    [Url(startsWith: "http", isRequired: false)]
    [ConfigurationKeyName("ERROR_WEBHOOK_URL")]
    public string? ErrorWebHookUrl { get; init; }

    [Required, Range(1, 5000)]
    [ConfigurationKeyName("START_PAGE")]
    public int StartPage { get; init; } = default!;

    [Required, Range(1, 5000)]
    [ConfigurationKeyName("TOTAL_PAGES")]
    public int TotalPages { get; init; } = default!;

    [Required]
    [ConfigurationKeyName("RUN_ON_STARTUP")]
    public bool RunOnStartup { get; init; }

    [ConfigurationKeyName("IGNORE_URLS")]
    public IReadOnlyCollection<string> IgnoreUrls { get; init; } = [];

    ///<summary>
    /// ADVANCED ENGINE SETTINGS
    ///</summary>
    [Required, Range(1, 200)]
    [ConfigurationKeyName("TOTAL_PARALLELISM_DEGREE")]
    public int ParallelismDegree { get; init; }

    [Required, Range(1, 1000)]
    [ConfigurationKeyName("PAGE_CRAWL_LIMIT")]
    public int PageCrawlLimit { get; init; }

    [Required]
    [ConfigurationKeyName("WAIT_ON_SCRAPING_PAGE_MILLISECONDS")]
    public int WaitOnScrapingPageMilliseconds { get; init; }

    internal bool IsWebhookEnabled => !string.IsNullOrWhiteSpace(WebHookUrl);
    internal bool IsErrorWebhookEnabled => !string.IsNullOrWhiteSpace(ErrorWebHookUrl);
}

internal class UrlAttribute(string startsWith, bool isRequired)
    : ValidationAttribute(errorMessage: $"Invalid URL, expected to start with {startsWith}")
{
    public override bool IsValid(object? value)
    {
        if (value == null)
        {
            return !isRequired;
        }

        if (value is not string valueAsString)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(valueAsString))
        {
            return !isRequired;
        }

        return valueAsString.StartsWith(startsWith, StringComparison.OrdinalIgnoreCase);
    }
}

internal class CronExpressionAttribute() 
    : ValidationAttribute(errorMessage: "Invalid CRON expression")
{
    public override bool IsValid(object? value)
    {
        return value is null || CronExpression.TryParse(value.ToString(), CronFormat.Standard, out _);
    }
}