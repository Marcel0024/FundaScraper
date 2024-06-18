using System.Text.Json;

namespace FundaScraper.App;

internal class WebhookTracker
{
    private readonly string DatabaseFilePath = Path.Combine("/data", "webhooks-history.json");
    private  Dictionary<string, Entry> Listings { get; init; }

    public WebhookTracker()
    {
        Listings = GetHistoryFile().GetAwaiter().GetResult().Listings;
    }

    public bool Contains(string name)
    {
        return Listings.ContainsKey(name);
    }

    internal async Task SaveWebHook(Entry entry)
    {
        Listings[entry.Name] = entry;

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

public record DbModel(Dictionary<string, Entry> Listings);
public record Entry(string Name, string Address, string Price, string Url, DateTimeOffset DateTimeAdded);