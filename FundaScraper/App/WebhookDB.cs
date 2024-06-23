﻿using System.Text.Json;

namespace FundaScraper.App;

internal class WebhookDB
{
    private readonly string WebhooksHistoryJson = Constants.FileNames.WebhooksHistoryJson;
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
        if (!File.Exists(WebhooksHistoryJson))
        {
            return new DbModel([]);
        }

        try
        {
            var json = await File.ReadAllTextAsync(WebhooksHistoryJson);
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

        Directory.CreateDirectory(Path.GetDirectoryName(WebhooksHistoryJson)!);

        await File.WriteAllTextAsync(WebhooksHistoryJson, json);
    }
}

public record DbModel(Dictionary<string, ListingModel> Listings);