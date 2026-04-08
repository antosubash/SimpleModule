using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SimpleModule.Database;

namespace SimpleModule.Host;

public class HostDbContextFactory : IDesignTimeDbContextFactory<HostDbContext>
{
    public HostDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var dbOptions =
            config.GetSection("Database").Get<DatabaseOptions>() ?? new DatabaseOptions();

        var optionsBuilder = new DbContextOptionsBuilder<HostDbContext>();
        var provider = DatabaseProviderDetector.Detect(
            dbOptions.DefaultConnection,
            dbOptions.Provider
        );

        switch (provider)
        {
            case DatabaseProvider.PostgreSql:
                optionsBuilder.UseNpgsql(dbOptions.DefaultConnection);
                break;
            case DatabaseProvider.SqlServer:
                optionsBuilder.UseSqlServer(dbOptions.DefaultConnection);
                break;
            default:
                optionsBuilder.UseSqlite(dbOptions.DefaultConnection);
                break;
        }

        optionsBuilder.UseOpenIddict();

        // Identity reads SchemaVersion from IdentityOptions.Stores via the
        // context's ApplicationServiceProvider. At design time there is no DI
        // container unless we supply one — without this the HostDbContext
        // model would be built against SchemaVersion 1 and omit the
        // Users_AspNetUserPasskeys table.
        var services = new ServiceCollection();
        services.Configure<IdentityOptions>(o =>
            o.Stores.SchemaVersion = IdentitySchemaVersions.Version3
        );
        optionsBuilder.UseApplicationServiceProvider(services.BuildServiceProvider());

        return new HostDbContext(optionsBuilder.Options, Options.Create(dbOptions));
    }
}
