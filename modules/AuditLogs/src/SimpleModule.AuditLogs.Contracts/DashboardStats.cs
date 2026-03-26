using SimpleModule.Core;

namespace SimpleModule.AuditLogs.Contracts;

[Dto]
public class DashboardStats
{
    public int TotalEntries { get; set; }
    public int UniqueUsers { get; set; }
    public double AverageDurationMs { get; set; }
    public double ErrorRate { get; set; }
    public Dictionary<string, int> BySource { get; set; } = new();
    public Dictionary<string, int> ByAction { get; set; } = new();
    public Dictionary<string, int> ByModule { get; set; } = new();
    public Dictionary<string, int> ByStatusCategory { get; set; } = new();
    public Dictionary<string, int> ByEntityType { get; set; } = new();
    public List<NamedCount> TopUsers { get; set; } = [];
    public List<NamedCount> TopPaths { get; set; } = [];
    public List<TimelinePoint> Timeline { get; set; } = [];
    public List<NamedCount> HourlyDistribution { get; set; } = [];
}

[Dto]
public class NamedCount
{
    public string Name { get; set; } = "";
    public int Count { get; set; }
}

[Dto]
public class TimelinePoint
{
    public string Date { get; set; } = "";
    public int Http { get; set; }
    public int Domain { get; set; }
    public int Changes { get; set; }
}
