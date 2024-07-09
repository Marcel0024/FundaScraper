using System.Text.Json;
using System.Threading.Channels;
using FundaScraper.Models;
using FundaScraper.Utilities;
using Microsoft.Extensions.Logging;

namespace FundaScraper.App;

internal class WebhookService(ILogger<WebhookService> logger)
{
    private readonly Channel<ListingModel> _webhookQueue = Channel.CreateUnbounded<ListingModel>();

    private readonly string _webhooksHistoryJsonFilePath = Constants.FileNames.WebhooksHistoryJson;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private Dictionary<string, ListingModel>? _listings;

    internal async Task Init(CancellationToken cancellationToken)
    {
        _listings = (await GetHistoryFile(cancellationToken)).Listings;
    }

    internal async Task Enqueue(ListingModel entry, CancellationToken cancellationToken)
    {
        if (entry is null
            || entry.Title is null
            || entry.Price is null
            || entry.Url is null)
        {
            logger.LogDebug("Empty object parsed");
            return;
        }

        if (HasSentBefore(entry))
        {
            return;
        }

        await _webhookQueue.Writer.WriteAsync(entry, cancellationToken);
    }

    internal IAsyncEnumerable<ListingModel> GetQueue(CancellationToken cancellationToken)
    {
        return _webhookQueue.Reader.ReadAllAsync(cancellationToken);
    }

    internal async Task MarkAsSent(ListingModel entry, CancellationToken cancellationToken)
    {
        _listings![entry.Url] = entry;

        await SaveHistoryFile(new DbModel(_listings), cancellationToken);
    }

    private bool HasSentBefore(ListingModel entry)
    {
        return _listings!.ContainsKey(entry.Url);
    }

    private async Task<DbModel> GetHistoryFile(CancellationToken cancellationToken)
    {
        if (!File.Exists(_webhooksHistoryJsonFilePath))
        {
            return new DbModel([]);
        }

        try
        {
            var json = await File.ReadAllTextAsync(_webhooksHistoryJsonFilePath, cancellationToken);
            return JsonSerializer.Deserialize<DbModel>(json)!;
        }
        catch
        {
            return new DbModel([]);
        }
    }

    private async Task SaveHistoryFile(DbModel model, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(model);

        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            await File.WriteAllTextAsync(_webhooksHistoryJsonFilePath, json, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

public record DbModel(Dictionary<string, ListingModel> Listings);