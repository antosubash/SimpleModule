namespace SimpleModule.Hosting.Maintenance;

public sealed class MaintenanceModeOptions
{
    /// <summary>
    /// Absolute path to the sentinel file. When unset the store resolves it to
    /// <c>{ContentRoot}/App_Data/maintenance.json</c>.
    /// </summary>
    public string? SentinelPath { get; set; }

    /// <summary>Name of the bypass cookie. Defaults to <c>sm_bypass</c>.</summary>
    public string BypassCookieName { get; set; } = "sm_bypass";

    /// <summary>Bypass cookie lifetime once issued. Default 12 hours.</summary>
    public TimeSpan BypassCookieLifetime { get; set; } = TimeSpan.FromHours(12);
}
