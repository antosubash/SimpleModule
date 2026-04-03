using SimpleModule.Core;

namespace SimpleModule.Email.Contracts;

[Dto]
public class EmailStats
{
    public int TotalSent { get; set; }
    public int TotalFailed { get; set; }
    public int TotalQueued { get; set; }
    public int TotalRetrying { get; set; }
    public int SentLast24Hours { get; set; }
    public int FailedLast24Hours { get; set; }
    public double FailureRateLast7Days { get; set; }
    public List<ErrorSummary> TopErrors { get; set; } = [];
    public List<DailyCount> DailyVolume { get; set; } = [];
}

[Dto]
public class ErrorSummary
{
    public string ErrorMessage { get; set; } = string.Empty;
    public int Count { get; set; }
}

[Dto]
public class DailyCount
{
    public DateTime Date { get; set; }
    public int Sent { get; set; }
    public int Failed { get; set; }
}
