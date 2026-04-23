using System.Diagnostics;
using System.Globalization;
using Bogus;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Database;
using SimpleModule.Products;
using SimpleModule.Products.Contracts;
using Spectre.Console;

namespace SimpleModule.PerfSeeder.Seeders;

internal sealed class ProductsSeeder(ProductsDbContext db, DatabaseProvider provider)
{
    /// <summary>
    /// Ids 1..10 are reserved for the migration seed data in <c>ProductConfiguration</c>.
    /// We skip these on truncate so the referential data stays intact.
    /// </summary>
    private const int MigrationSeedMaxId = 10;

    public async Task RunAsync(int count, int batchSize, int randomSeed, bool truncate)
    {
        AnsiConsole.MarkupLine($"[bold]-> Products[/] (target: {count:N0} rows)");

        if (truncate)
        {
            var table = db.QuoteTable(typeof(Product));
            // Table name comes from EF model metadata, not user input — safe from injection.
#pragma warning disable EF1002
            var deleted = await db
                .Database.ExecuteSqlRawAsync(
                    $"DELETE FROM {table} WHERE \"Id\" > {MigrationSeedMaxId}"
                )
                .ConfigureAwait(false);
#pragma warning restore EF1002
            AnsiConsole.MarkupLine($"[dim]  truncated {deleted:N0} existing rows[/]");
        }

        await EnableSqliteFastPathAsync(provider, db).ConfigureAwait(false);

        var faker = new Faker<Product>()
            .UseSeed(randomSeed)
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(
                p => p.Price,
                f =>
                    decimal.Parse(
                        f.Commerce.Price(1, 1000),
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture
                    )
            )
            .RuleFor(p => p.CreatedAt, f => f.Date.Past(1))
            .RuleFor(p => p.UpdatedAt, (_, p) => p.CreatedAt)
            .RuleFor(p => p.ConcurrencyStamp, _ => Guid.NewGuid().ToString("N"));

        var sw = Stopwatch.StartNew();
        var inserted = 0L;
        var remaining = count;
        var nextReport = (long)batchSize * 10;

        await using var tx = await db.Database.BeginTransactionAsync().ConfigureAwait(false);
        while (remaining > 0)
        {
            var take = Math.Min(batchSize, remaining);
            var batch = faker.Generate(take);
            await db.Products.AddRangeAsync(batch).ConfigureAwait(false);
            await db.SaveChangesAsync().ConfigureAwait(false);
            db.ChangeTracker.Clear();
            inserted += take;
            remaining -= take;

            if (inserted >= nextReport || remaining == 0)
            {
                SeederProgress.Report("products", inserted, count, sw);
                nextReport = inserted + (long)batchSize * 10;
            }
        }
        await tx.CommitAsync().ConfigureAwait(false);

        SeederProgress.Final("products", inserted, sw);
    }

    private static async Task EnableSqliteFastPathAsync(DatabaseProvider provider, DbContext ctx)
    {
        if (provider != DatabaseProvider.Sqlite)
        {
            return;
        }
        // WAL mode + relaxed sync drastically improves bulk insert throughput.
        // These pragmas persist for the connection lifetime only, so this is safe.
        await ctx.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;").ConfigureAwait(false);
        await ctx.Database.ExecuteSqlRawAsync("PRAGMA synchronous=NORMAL;").ConfigureAwait(false);
        await ctx.Database.ExecuteSqlRawAsync("PRAGMA temp_store=MEMORY;").ConfigureAwait(false);
    }
}
