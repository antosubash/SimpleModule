using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Database;

namespace SimpleModule.Hosting;

public class SimpleModuleOptions
{
    private readonly List<Action<IServiceCollection>> _moduleOptionsActions = [];

    /// <summary>
    /// Module assemblies to scan for Wolverine handlers. Set by the source-generated
    /// <c>AddSimpleModule()</c> from <c>ModuleExtensions.ModuleAssemblies</c>; not
    /// intended for user code.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public IReadOnlyList<Assembly> ModuleAssemblies { get; set; } = [];

    public bool EnableSwagger { get; set; } = true;

    public bool EnableHealthChecks { get; set; } = true;

    public bool EnableDevTools { get; set; } = true;

    /// <summary>
    /// Logs a warning at startup for every SimpleModule contract interface
    /// (e.g. <c>IUsersContracts</c>) that is in the assembly graph but has no
    /// registered implementation — the typical signal that a peer module's
    /// package is missing.
    /// Defaults to true in non-Production environments and false otherwise so
    /// production logs aren't polluted by intentionally-absent peers.
    /// </summary>
    public bool ValidateModuleGraph { get; set; } = true;

    /// <summary>
    /// Content Security Policy overrides. Modules can append extra origins for
    /// directives like <c>connect-src</c>, <c>img-src</c>, etc.
    /// </summary>
    public CspOptions Csp { get; } = new();

    /// <summary>
    /// The detected database provider, set during startup validation.
    /// </summary>
    internal DatabaseProvider DatabaseProvider { get; set; }

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
    public void ApplyModuleOptions(
        IServiceCollection services,
        Action<IServiceCollection> registerDefaults
    )
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
