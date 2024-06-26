using Cronos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FundaScraper.Utilities;

internal class CronPeriodicTimer(ILogger<CronPeriodicTimer> logger, IOptions<FundaScraperSettings> settings, TimeProvider timeProvider)
{
    private readonly CronExpression _cronExpression = CronExpression.Parse(settings.Value.CronExpression, CronFormat.Standard);

    public async ValueTask<bool> WaitForNextTickAsync(CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow();

        // Edge cases of timer ticking twice - and PeriodicTimer has a minimum period of 1ms
        utcNow.AddMilliseconds(500);

        var utcNext = _cronExpression.GetNextOccurrence(utcNow.UtcDateTime, TimeZoneInfo.Local)!;

        var interval = utcNext.Value - utcNow;

        using var timer = new PeriodicTimer(interval);

        logger.LogInformation("Next tick in {interval} at {utcNext}", interval, utcNext.Value.ToLocalTime());

        return await timer.WaitForNextTickAsync(cancellationToken);
    }
}
