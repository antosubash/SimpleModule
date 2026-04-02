using SimpleModule.Core.Authorization;

namespace SimpleModule.BackgroundJobs;

public sealed class BackgroundJobsPermissions : IModulePermissions
{
    public const string ViewJobs = "BackgroundJobs.ViewJobs";
    public const string ManageJobs = "BackgroundJobs.ManageJobs";
}
