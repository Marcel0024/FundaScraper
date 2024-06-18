using Amazon.Runtime.Internal.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using WebReaper.Sinks.Abstract;
using WebReaper.Sinks.Models;

namespace FundaScraper.App;

internal class WebhookSink(
    ILogger<WebhookSink> logger,
    IOptions<AppSettings> options,
    IHttpClientFactory httpClientFactory,
    WebhookTracker webhookTracker) : IScraperSink
{
    public bool DataCleanupOnStart { get; set; } = false;

    private readonly JsonSerializerOptions serializerOptions = new(JsonSerializerDefaults.Web);

    public async Task EmitAsync(ParsedData entity, CancellationToken cancellationToken = default)
    {
        if (!entity.Url.StartsWith("https://www.funda.nl/detail/koop"))
        {
            return;
        }

        var name = ParseValue(entity.Data["Name"]);
        var price = ParseValue(entity.Data["Price"]);

        if (webhookTracker.Contains(name))
        {
            return;
        }

        logger.LogInformation("Sending webhook of {0}.", name);

        var entry = new Entry(name, name, price, entity.Url, DateTimeOffset.Now);

        using var http = httpClientFactory.CreateClient();
        var response = await http.PostAsJsonAsync(options.Value.WebHookUrl, entry, serializerOptions, cancellationToken);

        response.EnsureSuccessStatusCode();

        await webhookTracker.SaveWebHook(entry);

        logger.LogInformation("Webhook sent and history saved");
    }

    private static string ParseValue(JToken? value)
    {
        return value?.ToString().Trim(['{', '}', ' ', '\n', '?']) ?? "";
    }
}

