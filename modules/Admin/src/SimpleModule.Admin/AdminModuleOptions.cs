using SimpleModule.Core;

namespace SimpleModule.Admin;

/// <summary>
/// Configurable options for the Admin module.
/// </summary>
public class AdminModuleOptions : IModuleOptions
{
    /// <summary>
    /// Number of users to display per page in the admin user list. Default: 20.
    /// </summary>
    public int UsersPageSize { get; set; } = 20;
}
