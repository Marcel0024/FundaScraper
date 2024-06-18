using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FundaScraper.App;

var builder = Host.CreateApplicationBuilder();

builder.Services.AddOptions<AppSettings>()
    .Bind(builder.Configuration)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<WebhookSink>();
builder.Services.AddSingleton<WebhookTracker>();

builder.Services.AddHttpClient();
builder.Services.AddHostedService<FundaScraperBackgroundService>();

// If the scraper crashes kill the app
builder.Services.Configure<HostOptions>(opts => opts.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost);

await builder.Build().RunAsync();
