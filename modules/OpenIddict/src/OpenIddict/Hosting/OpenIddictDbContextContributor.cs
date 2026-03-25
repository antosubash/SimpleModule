using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Core.Hosting;

namespace SimpleModule.OpenIddict.Hosting;

[SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Instantiated via DI")]
internal sealed class OpenIddictDbContextContributor : IHostDbContextContributor
{
    public void Configure(object options)
    {
        ((DbContextOptionsBuilder)options).UseOpenIddict();
    }
}
