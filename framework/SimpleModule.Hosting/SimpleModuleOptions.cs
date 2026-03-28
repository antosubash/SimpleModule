using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;

namespace SimpleModule.Hosting;

public class SimpleModuleOptions
{
    private readonly List<Action<IServiceCollection>> _moduleOptionsActions = [];

    public Type? ShellComponent { get; set; }

    public bool EnableSwagger { get; set; } = true;

    public bool EnableHealthChecks { get; set; } = true;

    public bool EnableDevTools { get; set; } = true;

    /// <summary>
    /// Configures options for a module. Called by generated Configure{Module}() extension methods.
    /// </summary>
    public SimpleModuleOptions ConfigureModule<TOptions>(Action<TOptions> configure)
        where TOptions : class, IModuleOptions
    {
        _moduleOptionsActions.Add(services => services.Configure(configure));
        return this;
    }

    /// <summary>
    /// Registers default options and applies user overrides. Called by generated code.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public void ApplyModuleOptions(IServiceCollection services, Action<IServiceCollection> registerDefaults)
    {
        // Register IOptions<T> defaults for all discovered options classes
        registerDefaults(services);

        // Apply user-provided overrides
        foreach (var action in _moduleOptionsActions)
        {
            action(services);
        }
    }
}
