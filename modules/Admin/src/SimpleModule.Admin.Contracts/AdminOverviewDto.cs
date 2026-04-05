using SimpleModule.Core;

namespace SimpleModule.Admin.Contracts;

[Dto]
public class AdminOverviewDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalRoles { get; set; }
}
