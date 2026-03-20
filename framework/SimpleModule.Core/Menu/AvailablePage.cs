namespace SimpleModule.Core.Menu;

public sealed class AvailablePage(string pageRoute, string viewPrefix, string module)
{
    public string PageRoute { get; } = pageRoute;
    public string ViewPrefix { get; } = viewPrefix;
    public string Module { get; } = module;
}
