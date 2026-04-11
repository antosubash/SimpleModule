using SimpleModule.Core.Entities;

namespace SimpleModule.BackgroundJobs.Entities;

public class JobProgress : Entity<Guid>
{
    public string JobTypeName { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public int ProgressPercentage { get; set; }
    public string? ProgressMessage { get; set; }
    public string? Data { get; set; }
    public string? Logs { get; set; }
}
