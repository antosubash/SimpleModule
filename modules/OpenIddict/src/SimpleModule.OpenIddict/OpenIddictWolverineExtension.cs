using Wolverine;
using Wolverine.Attributes;

[assembly: WolverineModule(typeof(SimpleModule.OpenIddict.OpenIddictWolverineExtension))]

namespace SimpleModule.OpenIddict;

#pragma warning disable CA1812 // Instantiated by Wolverine via [WolverineModule]
internal sealed class OpenIddictWolverineExtension : IWolverineExtension
#pragma warning restore CA1812
{
    public void Configure(WolverineOptions options)
    {
        options.Discovery.IncludeAssembly(typeof(OpenIddictWolverineExtension).Assembly);
    }
}
