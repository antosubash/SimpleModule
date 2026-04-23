using System.Diagnostics;
using Bogus;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Database;
using SimpleModule.Orders;
using SimpleModule.Orders.Contracts;
using Spectre.Console;

namespace SimpleModule.PerfSeeder.Seeders;

internal sealed class OrdersSeeder(OrdersDbContext db, DatabaseProvider provider)
{
    private const int UserPoolSize = 1000;
    private const int ProductIdMax = 1000;
    private const int MaxItemsPerOrder = 5;

    public async Task RunAsync(int count, int batchSize, int randomSeed, bool truncate)
    {
        AnsiConsole.MarkupLine($"[bold]-> Orders[/] (target: {count:N0} rows)");

        if (truncate)
        {
            var itemsTable = db.QuoteTable(typeof(OrderItem));
            var ordersTable = db.QuoteTable(typeof(Order));
            // Delete children first — the FK from OrderItems.OrderId to Orders is restrict by default.
            // Table names come from EF model metadata, not user input — safe from injection.
#pragma warning disable EF1002
            var deletedItems = await db
                .Database.ExecuteSqlRawAsync($"DELETE FROM {itemsTable}")
                .ConfigureAwait(false);
            var deletedOrders = await db
                .Database.ExecuteSqlRawAsync($"DELETE FROM {ordersTable}")
                .ConfigureAwait(false);
#pragma warning restore EF1002
            AnsiConsole.MarkupLine(
                $"[dim]  truncated {deletedOrders:N0} orders, {deletedItems:N0} items[/]"
            );
        }

        await EnableSqliteFastPathAsync(provider, db).ConfigureAwait(false);

        var rng = new Randomizer(randomSeed);
        var now = DateTimeOffset.UtcNow;

        // Build the faker inline so we can capture rng for deterministic distinct-product sampling.
        Order BuildOrder()
        {
            var itemCount = rng.Int(1, MaxItemsPerOrder);
            // Distinct ProductIds within an order (composite PK is OrderId, ProductId).
            var pickedProducts = new HashSet<int>(itemCount);
            var items = new List<OrderItem>(itemCount);
            while (pickedProducts.Count < itemCount)
            {
                var pid = rng.Int(1, ProductIdMax);
                if (pickedProducts.Add(pid))
                {
                    items.Add(new OrderItem { ProductId = pid, Quantity = rng.Int(1, 10) });
                }
            }
            return new Order
            {
                UserId = rng.Int(1, UserPoolSize)
                    .ToString(System.Globalization.CultureInfo.InvariantCulture),
                Items = items,
                Total = Math.Round((decimal)rng.Double(10, 500), 2),
                CreatedAt = now.AddMinutes(-rng.Int(0, 60 * 24 * 30)),
                UpdatedAt = now,
                CreatedBy = "perf-seeder",
                UpdatedBy = "perf-seeder",
                ConcurrencyStamp = Guid.NewGuid().ToString("N"),
            };
        }

        var sw = Stopwatch.StartNew();
        var inserted = 0L;
        var itemsInserted = 0L;
        var remaining = count;
        var nextReport = (long)batchSize * 5;

        await using var tx = await db.Database.BeginTransactionAsync().ConfigureAwait(false);
        while (remaining > 0)
        {
            var take = Math.Min(batchSize, remaining);
            var batch = new List<Order>(take);
            for (var i = 0; i < take; i++)
            {
                var o = BuildOrder();
                batch.Add(o);
                itemsInserted += o.Items.Count;
            }
            await db.Orders.AddRangeAsync(batch).ConfigureAwait(false);
            await db.SaveChangesAsync().ConfigureAwait(false);
            db.ChangeTracker.Clear();
            inserted += take;
            remaining -= take;

            if (inserted >= nextReport || remaining == 0)
            {
                SeederProgress.Report("orders", inserted, count, sw);
                nextReport = inserted + (long)batchSize * 5;
            }
        }
        await tx.CommitAsync().ConfigureAwait(false);

        SeederProgress.Final($"orders ({itemsInserted:N0} items)", inserted, sw);
    }

    private static async Task EnableSqliteFastPathAsync(DatabaseProvider provider, DbContext ctx)
    {
        if (provider != DatabaseProvider.Sqlite)
        {
            return;
        }
        await ctx.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;").ConfigureAwait(false);
        await ctx.Database.ExecuteSqlRawAsync("PRAGMA synchronous=NORMAL;").ConfigureAwait(false);
        await ctx.Database.ExecuteSqlRawAsync("PRAGMA temp_store=MEMORY;").ConfigureAwait(false);
    }
}
