using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SimpleModule.Hosting;

/// <summary>
/// Boot-time validator that catches the most common "missing peer module" failure mode:
/// a module's <c>SimpleModule.X.Contracts</c> assembly is loaded (because some other
/// module references it) but no service satisfies the contract interface, meaning the
/// implementing <c>SimpleModule.X</c> package was never installed.
/// </summary>
#pragma warning disable CA1812 // Instantiated by DI as IHostedService
internal sealed class ModuleGraphValidator : IHostedService
#pragma warning restore CA1812
{
    private const string SimpleModulePrefix = "SimpleModule.";
    private const string ContractsSuffix = ".Contracts";

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ModuleGraphValidator> _logger;
    private readonly SimpleModuleOptions _options;
    private readonly IHostEnvironment _environment;

    public ModuleGraphValidator(
        IServiceProvider serviceProvider,
        ILogger<ModuleGraphValidator> logger,
        SimpleModuleOptions options,
        IHostEnvironment environment
    )
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options;
        _environment = environment;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.ValidateModuleGraph)
        {
            return Task.CompletedTask;
        }

        // In Production this check is too noisy — downstream apps may legitimately
        // ship a contracts assembly without the impl (e.g. their own implementation).
        if (_environment.IsProduction())
        {
            return Task.CompletedTask;
        }

        var unsatisfied = FindUnsatisfiedContracts();
        if (unsatisfied.Count == 0)
        {
            return Task.CompletedTask;
        }

        foreach (var (contractFullName, suspectedModulePackage) in unsatisfied)
        {
            _logger.LogWarning(
                "Module graph: contract {Contract} is referenced but no implementation is registered. The module providing it ({Package}) appears to be missing — add a PackageReference (or call its registration extension) to fix runtime resolution failures.",
                contractFullName,
                suspectedModulePackage
            );
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private List<(string Contract, string Package)> FindUnsatisfiedContracts()
    {
        var unsatisfied = new List<(string Contract, string Package)>();

        using var scope = _serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;

        foreach (var assembly in EnumerateContractsAssemblies())
        {
            var assemblyName = assembly.GetName().Name!;
            var implPackage = assemblyName[..^ContractsSuffix.Length];

            foreach (var iface in EnumerateContractInterfaces(assembly))
            {
                if (sp.GetService(iface) is null)
                {
                    unsatisfied.Add((iface.FullName ?? iface.Name, implPackage));
                }
            }
        }

        return unsatisfied;
    }

    private static IEnumerable<Assembly> EnumerateContractsAssemblies()
    {
        return AppDomain
            .CurrentDomain.GetAssemblies()
            .Where(a =>
            {
                var name = a.GetName().Name;
                return name is not null
                    && name.StartsWith(SimpleModulePrefix, StringComparison.Ordinal)
                    && name.EndsWith(ContractsSuffix, StringComparison.Ordinal);
            });
    }

    private static IEnumerable<Type> EnumerateContractInterfaces(Assembly assembly)
    {
        Type[] exported;
        try
        {
            exported = assembly.GetExportedTypes();
        }
        catch (ReflectionTypeLoadException)
        {
            return [];
        }

        return exported.Where(t =>
            t.IsInterface
            && t.Name.StartsWith('I')
            && t.Name.EndsWith("Contracts", StringComparison.Ordinal)
        );
    }
}
