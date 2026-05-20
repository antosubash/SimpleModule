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
}
