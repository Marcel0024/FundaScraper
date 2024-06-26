using FundaScraper.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using WebReaper.Sinks.Abstract;
using WebReaper.Sinks.Models;

namespace FundaScraper.App;

internal class WebhookSink(
    ILogger<WebhookSink> logger,
    IOptions<FundaScraperSettings> settings,
    IHttpClientFactory httpClientFactory,
    TimeProvider timeProvider,
    WebhookPersistanceTracker webhookPersistance) : IScraperSink
{
    public bool DataCleanupOnStart { get; set; } = false;

    private readonly JsonSerializerOptions serializerOptions = new(JsonSerializerDefaults.Web);
    private readonly string? WebHookUrl = settings.Value.WebHookUrl;
    private readonly bool IsWebhookEnabled = !string.IsNullOrWhiteSpace(settings.Value.WebHookUrl);

    public async Task EmitAsync(ParsedData entity, CancellationToken cancellationToken = default)
    {
        if (!IsWebhookEnabled)
        {
            return;
        }

        var listingModel = JsonSerializer.Deserialize<ListingModel>(entity.Data.ToString())!;

        listingModel.Url = entity.Url;
        listingModel.DateTimeAdded = timeProvider.GetLocalNow();

        if (listingModel is null
            || listingModel.Title is null
            || listingModel.Price is null
            || webhookPersistance.HasSentBefore(listingModel))
        {
            return;
        }

        logger.LogInformation("Sending webhook of {title}.", listingModel.Title);

        using var http = httpClientFactory.CreateClient();
        var response = await http.PostAsJsonAsync(WebHookUrl, listingModel, serializerOptions, cancellationToken);

        response.EnsureSuccessStatusCode();

        await webhookPersistance.MarkAsSent(listingModel);

        logger.LogInformation("Webhook sent and history saved");
    }
}

