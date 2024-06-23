using Cronos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FundaScraper.Utilities;

internal class CronPeriodicTimer(ILogger<CronPeriodicTimer> logger, IOptions<FundaScraperSettings> settings)
{
    private readonly CronExpression _cronExpression = CronExpression.Parse(settings.Value.CronExpression, CronFormat.Standard);

    public async ValueTask<bool> WaitForNextTickAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var utcNow = DateTime.UtcNow;
        var minDelay = TimeSpan.FromMilliseconds(500);
        var utcNext = _cronExpression.GetNextOccurrence(utcNow + minDelay)!;

        var interval = utcNext.Value - utcNow;

        using var timer = new PeriodicTimer(interval);

        logger.LogInformation("Next tick in {interval} at {utcNext}", interval, utcNext);

        return await timer.WaitForNextTickAsync(cancellationToken);
    }
}
