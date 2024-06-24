namespace FundaScraper;

internal static class Constants
{
    internal static class FileNames
    {
        private static string BasePath = Path.Combine("/", "data");
        internal static string WebhooksHistoryJson = Path.Combine(BasePath, "webhooks-history.json");
        internal static string ResultsFilePath = Path.Combine(BasePath, "results.csv");
    }
}
