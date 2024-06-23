using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;

namespace FundaScraper;

internal class FundaScraperSettings
{
    /// <summary>
    /// ENGINE SETTINGS
    /// </summary>
    /// 
    [ConfigurationKeyName("CRON")] public string CronExpression { get; init; } = default!;
    
    [Required, FundaUrl]
    [ConfigurationKeyName("FUNDA_URL")] public required string FundaUrl { get; init; }

    [OptionalUrl]
    [ConfigurationKeyName("WEBHOOK_URL")] public string? WebHookUrl { get; init; }

    [OptionalUrl]
    [ConfigurationKeyName("ERROR_WEBHOOK_URL")] public string? ErrorWebHookUrl { get; init; }

    [Range(1, 5000)]
    [ConfigurationKeyName("START_PAGE")] public int StartPage { get; init; } = default!;

    [Range(1, 5000)]
    [ConfigurationKeyName("TOTAL_PAGES")] public int TotalPages { get; init; } = default!;

    [ConfigurationKeyName("RUN_ON_STARTUP")] public bool RunOnStartup { get; init; }

    [Range(1, 1000)]
    [ConfigurationKeyName("PAGE_CRAWL_LIMIT")] public int PageCrawlLimit { get; init; }

    [Range(1, 1000)]
    [ConfigurationKeyName("TOTAL_PARALLELISM_DEGREE")] public int ParallelismDegree { get; init; }

    /// <summary>
    /// SELECTORS
    /// </summary>
    [ConfigurationKeyName("LISTING_SELECTOR")] public string ListingLinkSelector { get; set; } = default!;
    [ConfigurationKeyName("TITLE_SELECTOR")] public string TitleSelector { get; set; } = default!;
    [ConfigurationKeyName("ZIP_CODE_SELECTOR")] public string ZipCodeSelector { get; set; } = default!;
    [ConfigurationKeyName("PRICE_SELECTOR")] public string PriceSelector { get; set; } = default!;
    [ConfigurationKeyName("AREA_SELECTOR")] public string AreaSelector { get; set; } = default!;
    [ConfigurationKeyName("TOTAL_ROOMS_SELECTOR")] public string TotalRoomsSelector { get; set; } = default!;
}

internal class FundaUrlAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value == null)
        {
            return true;
        }

        return value is string valueAsString && valueAsString.StartsWith("https://www.funda.nl/", StringComparison.OrdinalIgnoreCase);
    }
}

internal class OptionalUrlAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is null || value is not string valueAsString || string.IsNullOrWhiteSpace(valueAsString))
        {
            return true;
        }

        return valueAsString.StartsWith("https://www.funda.nl/", StringComparison.OrdinalIgnoreCase);
    }
}