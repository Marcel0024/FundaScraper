using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace FundaScraper.App;

internal class WebhookSenderBackgroundService(
    ILogger<WebhookSenderBackgroundService> logger,
    IHttpClientFactory httpClientFactory,
    IOptions<FundaScraperSettings> settings,
    WebhookService webhookPersistanceTracker
    ) : BackgroundService
{
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (!settings.Value.IsWebhookEnabled)
        {
            return;
        }

        await foreach (var listingModel in webhookPersistanceTracker.GetQueue(cancellationToken))
        {
            using var http = httpClientFactory.CreateClient();

            logger.LogInformation("Sending webhook {}", listingModel.Title);

            var response = await http.PostAsJsonAsync(settings.Value.WebHookUrl!, listingModel, _serializerOptions, cancellationToken);

            response.EnsureSuccessStatusCode();

            await webhookPersistanceTracker.MarkAsSent(listingModel, cancellationToken);

            await Task.Delay(TimeSpan.FromSeconds(value: 3), cancellationToken);
        }
    }
}
