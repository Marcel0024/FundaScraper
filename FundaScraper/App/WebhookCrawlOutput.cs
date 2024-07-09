using CocoCrawler.Outputs;
using FundaScraper.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Text.Json;

namespace FundaScraper.App;

internal class WebhookCrawlOutput(
    IOptions<FundaScraperSettings> settings,
    TimeProvider timeProvider,
    WebhookService webhookService) : ICrawlOutput
{
    public Task Initiaize(CancellationToken cancellationToken) => webhookService.Init(cancellationToken);

    public async Task WriteAsync(JObject jObject, CancellationToken cancellationToken)
    {
        if (!settings.Value.IsWebhookEnabled)
        {
            return;
        }

        var listingModel = JsonSerializer.Deserialize<ListingModel>(jObject.ToString())!;
        listingModel.DateTimeAdded = timeProvider.GetLocalNow();

        await webhookService.Enqueue(listingModel, cancellationToken);
    }
}

