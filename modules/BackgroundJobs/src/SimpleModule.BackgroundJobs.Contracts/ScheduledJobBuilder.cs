using System.Globalization;

namespace SimpleModule.BackgroundJobs.Contracts;

/// <summary>
/// Default fluent builder backing <see cref="IScheduledJob{TJob}"/>. Each method
/// mutates the supplied <see cref="ScheduledJobDefinition"/> and returns the same
/// instance. Cron parsing is intentionally not performed here so the Contracts
/// assembly stays free of the Cronos dependency; the scheduler validates and
/// reports per-definition errors at tick time.
/// </summary>
internal sealed class ScheduledJobBuilder<TJob>(ScheduledJobDefinition definition)
    : IScheduledJob<TJob>
    where TJob : IModuleJob
{
    public IScheduledJob<TJob> Cron(string expression)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);
        definition.CronExpression = expression;
        return this;
    }

    public IScheduledJob<TJob> EveryMinutes(int minutes)
    {
        if (minutes < 1 || minutes > 59)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minutes),
                "EveryMinutes requires a value between 1 and 59."
            );
        }
        return Cron($"*/{minutes.ToString(CultureInfo.InvariantCulture)} * * * *");
    }

    public IScheduledJob<TJob> Hourly() => Cron("0 * * * *");

    public IScheduledJob<TJob> Daily() => Cron("0 0 * * *");

    public IScheduledJob<TJob> DailyAt(string time)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(time);
        if (
            !TimeOnly.TryParseExact(
                time,
                "HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed
            )
        )
        {
            throw new ArgumentException(
                $"DailyAt expects a 24h 'HH:mm' value, got '{time}'.",
                nameof(time)
            );
        }
        return Cron(
            $"{parsed.Minute.ToString(CultureInfo.InvariantCulture)} {parsed.Hour.ToString(CultureInfo.InvariantCulture)} * * *"
        );
    }

    public IScheduledJob<TJob> Weekdays() => Cron("0 0 * * MON-FRI");

    public IScheduledJob<TJob> Timezone(string tz)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tz);
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(tz);
        }
        catch (TimeZoneNotFoundException ex)
        {
            throw new ArgumentException($"Unknown timezone '{tz}'.", nameof(tz), ex);
        }
        definition.TimeZoneId = tz;
        return this;
    }

    public IScheduledJob<TJob> WithoutOverlapping()
    {
        definition.WithoutOverlapping = true;
        return this;
    }

    public IScheduledJob<TJob> OnOneServer()
    {
        definition.OnOneServer = true;
        return this;
    }

    public IScheduledJob<TJob> WithPayload(object payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        definition.Payload = payload;
        return this;
    }
}
