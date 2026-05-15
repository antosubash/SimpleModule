namespace SimpleModule.Hosting.Maintenance;

/// <summary>
/// Configures the file-based maintenance sentinel. The default sentinel lives
/// alongside the running app's content root and is created by the
/// <c>sm down</c> CLI command at deploy time.
/// </summary>
public sealed class MaintenanceModeOptions
{
    /// <summary>
    /// Filename of the sentinel file relative to the content root.
    /// Mirrors the Laravel convention of a hidden file at the app root.
    /// </summary>
    public string SentinelFileName { get; set; } = ".maintenance";

    /// <summary>
    /// How long to cache sentinel reads in memory before re-checking the
    /// file system. Bounds the time between flipping the sentinel and the
    /// middleware noticing.
    /// </summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Name of the cookie that records a verified bypass. HttpOnly, Secure,
    /// SameSite=Lax.
    /// </summary>
    public string BypassCookieName { get; set; } = "sm_bypass";

    /// <summary>
    /// Lifetime of the bypass cookie. After this elapses, the bypass query
    /// parameter must be re-presented.
    /// </summary>
    public TimeSpan BypassCookieLifetime { get; set; } = TimeSpan.FromHours(12);
}
