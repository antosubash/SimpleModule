namespace SimpleModule.DevTools;

/// <summary>
/// Shared constants between DevTools and Hosting to avoid stringly-typed coupling.
/// </summary>
public static class DevToolsConstants
{
    /// <summary>
    /// HttpContext.Items key set by <see cref="ViteDevMiddleware"/> when the Vite
    /// dev server is detected, read by the Inertia page renderer to switch HTML mode.
    /// </summary>
    public const string ViteDevServerKey = "ViteDevServer";
}
