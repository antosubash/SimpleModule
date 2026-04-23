using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SimpleModule.Database;
using SimpleModule.Orders;
using SimpleModule.PerfSeeder.Seeders;
using SimpleModule.Products;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.PerfSeeder;

public sealed class SeedCommand : AsyncCommand<SeedSettings>
{
    private const int DefaultProductsCount = 1_000_000;
    private const int DefaultOrdersCount = 100_000;

    public override async Task<int> ExecuteAsync(CommandContext context, SeedSettings settings)
    {
        var projectPath = ResolveProjectPath(settings.ProjectPath);
        if (projectPath is null)
        {
            AnsiConsole.MarkupLine(
                "[red]Could not find a host project. Pass --project <path-to-host-project>.[/]"
            );
            return 1;
        }

        var dbOptions = LoadDatabaseOptions(projectPath, settings);
        if (string.IsNullOrWhiteSpace(dbOptions.DefaultConnection))
        {
            AnsiConsole.MarkupLine(
                "[red]Database:DefaultConnection is empty. Configure appsettings.json or pass --connection.[/]"
            );
            return 1;
        }

        DatabaseProvider provider;
        try
        {
            provider = DatabaseProviderDetector.Detect(
                dbOptions.DefaultConnection,
                dbOptions.Provider
            );
        }
        catch (InvalidOperationException ex)
        {
            AnsiConsole.MarkupLine($"[red]{ex.Message.EscapeMarkup()}[/]");
            return 1;
        }

        AnsiConsole.MarkupLine(
            $"[bold]Perf seeder[/] — project: [cyan]{projectPath.EscapeMarkup()}[/], provider: [cyan]{provider}[/]"
        );
        AnsiConsole.MarkupLine(
            $"[dim]Connection: {Redact(dbOptions.DefaultConnection).EscapeMarkup()}[/]"
        );

        var module = settings.Module.Trim();
        var runAll = module.Length == 0 || module.Equals("all", StringComparison.OrdinalIgnoreCase);
        var runProducts = runAll || module.Equals("products", StringComparison.OrdinalIgnoreCase);
        var runOrders = runAll || module.Equals("orders", StringComparison.OrdinalIgnoreCase);

        if (!runProducts && !runOrders)
        {
            AnsiConsole.MarkupLine(
                $"[red]Unknown module '{module.EscapeMarkup()}'. Valid: products, orders, all.[/]"
            );
            return 1;
        }

        var totalStopwatch = Stopwatch.StartNew();

        if (runProducts)
        {
            var count = settings.Count ?? DefaultProductsCount;
            using var db = BuildContext<ProductsDbContext>(dbOptions, provider);
            if (settings.CreateSchema)
            {
                EnsureTablesCreated(db);
            }
            await new ProductsSeeder(db, provider)
                .RunAsync(count, settings.BatchSize, settings.RandomSeed, settings.Truncate)
                .ConfigureAwait(false);
        }

        if (runOrders)
        {
            var count = settings.Count ?? DefaultOrdersCount;
            using var db = BuildContext<OrdersDbContext>(dbOptions, provider);
            if (settings.CreateSchema)
            {
                EnsureTablesCreated(db);
            }
            await new OrdersSeeder(db, provider)
                .RunAsync(count, settings.BatchSize, settings.RandomSeed, settings.Truncate)
                .ConfigureAwait(false);
        }

        totalStopwatch.Stop();
        AnsiConsole.MarkupLine($"[green]Done in {totalStopwatch.Elapsed.TotalSeconds:F1}s.[/]");
        return 0;
    }

    private static string? ResolveProjectPath(string? explicitPath)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            return Path.GetFullPath(explicitPath);
        }

        var dir = Directory.GetCurrentDirectory();
        while (dir is not null)
        {
            if (Directory.GetFiles(dir, "*.slnx").Length > 0)
            {
                var candidate = Path.Combine(dir, "template", "SimpleModule.Host");
                if (File.Exists(Path.Combine(candidate, "appsettings.json")))
                {
                    return candidate;
                }

                var srcHosts = Directory.GetDirectories(Path.Combine(dir, "src"), "*.Host");
                foreach (var host in srcHosts)
                {
                    if (File.Exists(Path.Combine(host, "appsettings.json")))
                    {
                        return host;
                    }
                }
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        return null;
    }

    private static DatabaseOptions LoadDatabaseOptions(string projectPath, SeedSettings settings)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(projectPath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var options = config.GetSection("Database").Get<DatabaseOptions>() ?? new DatabaseOptions();

        if (!string.IsNullOrWhiteSpace(settings.Connection))
        {
            options.DefaultConnection = settings.Connection;
        }
        if (!string.IsNullOrWhiteSpace(settings.Provider))
        {
            options.Provider = settings.Provider;
        }

        return options;
    }

    private static TContext BuildContext<TContext>(
        DatabaseOptions dbOptions,
        DatabaseProvider provider
    )
        where TContext : DbContext
    {
        var builder = new DbContextOptionsBuilder<TContext>();
        switch (provider)
        {
            case DatabaseProvider.PostgreSql:
                builder.UseNpgsql(dbOptions.DefaultConnection);
                break;
            case DatabaseProvider.SqlServer:
                builder.UseSqlServer(dbOptions.DefaultConnection);
                break;
            default:
                builder.UseSqlite(dbOptions.DefaultConnection);
                break;
        }

        var ctx = (TContext?)
            Activator.CreateInstance(typeof(TContext), builder.Options, Options.Create(dbOptions));
        if (ctx is null)
        {
            throw new InvalidOperationException(
                $"Could not construct DbContext of type {typeof(TContext).Name}."
            );
        }

        ctx.ChangeTracker.AutoDetectChangesEnabled = false;
        ctx.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        return ctx;
    }

    /// <summary>
    /// Creates the target DbContext's tables even when the physical DB already exists
    /// (EnsureCreated skips table creation when any tables are present). Swallows
    /// duplicate-table errors so it's safe to call across multiple module contexts
    /// sharing the same database.
    /// </summary>
    private static void EnsureTablesCreated(DbContext db)
    {
        if (db.Database.EnsureCreated())
        {
            return;
        }
        try
        {
            db.GetService<IRelationalDatabaseCreator>().CreateTables();
        }
        catch (DbException)
        {
            // Some/all tables already exist — fine.
        }
    }

    private static string Redact(string connectionString)
    {
        // Hide password-like values in log output.
        var parts = connectionString.Split(';');
        for (var i = 0; i < parts.Length; i++)
        {
            var kv = parts[i].Split('=', 2);
            if (
                kv.Length == 2
                && kv[0].Trim().Equals("Password", StringComparison.OrdinalIgnoreCase)
            )
            {
                parts[i] = $"{kv[0]}=***";
            }
        }
        return string.Join(';', parts);
    }
}
