using Cronos;

namespace SimpleModule.BackgroundJobs.Scheduler;

internal static class CronCalculator
{
    public static CronExpression Parse(string expression)
    {
        var format =
            expression.Split(' ').Length > 5 ? CronFormat.IncludeSeconds : CronFormat.Standard;
        return CronExpression.Parse(expression, format);
    }

    public static DateTimeOffset? GetNextOccurrence(
        string expression,
        string timeZoneId,
        DateTimeOffset fromInclusiveExclusive
    )
    {
        var cron = Parse(expression);
        var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        return cron.GetNextOccurrence(fromInclusiveExclusive, tz, inclusive: false);
    }

    /// <summary>UTC-only next-occurrence helper for the legacy recurring path.</summary>
    public static DateTimeOffset? GetNextOccurrenceUtc(string expression, DateTime fromUtc)
    {
        var next = Parse(expression).GetNextOccurrence(fromUtc, inclusive: false);
        return next is null ? null : new DateTimeOffset(next.Value, TimeSpan.Zero);
    }
}
