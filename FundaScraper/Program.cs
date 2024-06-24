using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FundaScraper.App;
using Microsoft.Extensions.Configuration;
using FundaScraper;
using FundaScraper.Utilities;

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
builder.Services.AddSingleton<WebhookSink>();
builder.Services.AddSingleton<WebhookPersistanceTracker>();

builder.Services.AddHostedService<FundaScraperBackgroundService>();

// If the scraper crashes kill the app
builder.Services.Configure<HostOptions>(opts => opts.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost);

await builder.Build().RunAsync();
