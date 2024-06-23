using System.Text.Json;

namespace FundaScraper.App;

internal class WebhookDB
{
    private readonly string DatabaseFilePath = Path.Combine("/data", "webhooks-history.json");
    private  Dictionary<string, ListingModel> Listings { get; init; }

    public WebhookDB()
    {
        Listings = GetHistoryFile().GetAwaiter().GetResult().Listings;
    }

    public bool Contains(ListingModel entry)
    {
        return Listings.ContainsKey(entry.Url);
    }

    internal async Task SaveWebHook(ListingModel entry)
    {
        Listings[entry.Url] = entry;

        await SaveHistoryFile(new DbModel(Listings));
    }

    private async Task<DbModel> GetHistoryFile()
    {
        if (!File.Exists(DatabaseFilePath))
        {
            return new DbModel([]);
        }

        try
        {
            var json = await File.ReadAllTextAsync(DatabaseFilePath);
            return JsonSerializer.Deserialize<DbModel>(json)!;
        }
        catch
        {
            return new DbModel([]);
        }
    }

    private async Task SaveHistoryFile(DbModel model)
    {
        var json = JsonSerializer.Serialize(model);

        Directory.CreateDirectory(Path.GetDirectoryName(DatabaseFilePath)!);

        await File.WriteAllTextAsync(DatabaseFilePath, json);
    }
}

public record DbModel(Dictionary<string, ListingModel> Listings);