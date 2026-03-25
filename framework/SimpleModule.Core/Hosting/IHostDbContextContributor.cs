namespace SimpleModule.Core.Hosting;

/// <summary>
/// Allows modules to contribute configuration to the host DbContext options builder.
/// The parameter is <c>Microsoft.EntityFrameworkCore.DbContextOptionsBuilder</c>.
/// Core cannot reference EF Core directly, so <c>object</c> is used.
/// Register implementations in <see cref="IModule.ConfigureServices"/> via DI.
/// </summary>
public interface IHostDbContextContributor
{
    void Configure(object options);
}
