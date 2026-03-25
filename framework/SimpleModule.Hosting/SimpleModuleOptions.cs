namespace SimpleModule.Hosting;

public class SimpleModuleOptions
{
    public Type? ShellComponent { get; set; }

    public bool EnableSwagger { get; set; } = true;

    public bool EnableHealthChecks { get; set; } = true;

    public bool EnableDevTools { get; set; } = true;
}
