using FundaScraper;
using FundaScraper.App;
using FundaScraper.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder();

builder.Configuration.AddJsonFile("defaults.json", optional: false);
builder.Configuration.AddJsonFile($"defaults.{builder.Environment.EnvironmentName}.json", optional: true);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddOptions<FundaScraperSettings>()
    .Bind(builder.Configuration)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<CronPeriodicTimer>();

builder.Services.AddSingleton<WebhookCrawlOutput>();
builder.Services.AddSingleton<WebhookService>();
builder.Services.AddHostedService<WebhookSenderBackgroundService>();

builder.Services.AddHostedService<FundaScraperBackgroundService>();

// If the scraper crashes kill the app
builder.Services.Configure<HostOptions>(opts => opts.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost);

await builder.Build().RunAsync();
