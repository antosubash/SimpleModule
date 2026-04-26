using Wolverine;
using Wolverine.Attributes;

[assembly: WolverineModule(typeof(SimpleModule.AuditLogs.AuditLogsWolverineExtension))]

namespace SimpleModule.AuditLogs;

#pragma warning disable CA1812 // Instantiated by Wolverine via [WolverineModule]
internal sealed class AuditLogsWolverineExtension : IWolverineExtension
#pragma warning restore CA1812
{
    public void Configure(WolverineOptions options)
    {
        options.Discovery.IncludeAssembly(typeof(AuditLogsWolverineExtension).Assembly);
    }
}
