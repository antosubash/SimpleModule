using System.Diagnostics;
using Bogus;
using Microsoft.EntityFrameworkCore;
using SimpleModule.AuditLogs;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.Database;
using Spectre.Console;

namespace SimpleModule.PerfSeeder.Seeders;

internal sealed class AuditLogsSeeder(AuditLogsDbContext db, DatabaseProvider provider)
{
    private static readonly string[] Modules =
    [
        "Products",
        "Orders",
        "Users",
        "AuditLogs",
        "Permissions",
        "Settings",
        "FileStorage",
        "PageBuilder",
    ];

    private static readonly string[] HttpMethods = ["GET", "POST", "PUT", "DELETE"];

    private static readonly string[] Paths =
    [
        "/api/products",
        "/api/orders",
        "/api/users",
        "/api/audit-logs",
        "/api/settings",
        "/api/files",
        "/login",
        "/logout",
    ];

    private static readonly int[] StatusCodes = [200, 201, 204, 400, 401, 403, 404, 500];
    private static readonly float[] StatusWeights =
    [
        0.55f,
        0.10f,
        0.05f,
        0.08f,
        0.05f,
        0.03f,
        0.10f,
        0.04f,
    ];
    private static readonly string[] EntityTypes = ["Product", "Order", "User", "File"];
    private static readonly AuditAction[] AllActions = Enum.GetValues<AuditAction>();
    private static readonly AuditSource[] AllSources = Enum.GetValues<AuditSource>();

    public async Task RunAsync(int count, int batchSize, int randomSeed, bool truncate)
    {
        AnsiConsole.MarkupLine($"[bold]-> AuditLogs[/] (target: {count:N0} rows)");

        if (truncate)
        {
            var table = db.QuoteTable(typeof(AuditEntry));
            // Table name comes from EF model metadata, not user input — safe from injection.
#pragma warning disable EF1002
            var deleted = await db
                .Database.ExecuteSqlRawAsync($"DELETE FROM {table}")
                .ConfigureAwait(false);
#pragma warning restore EF1002
            AnsiConsole.MarkupLine($"[dim]  truncated {deleted:N0} existing rows[/]");
        }

        await EnableSqliteFastPathAsync(provider, db).ConfigureAwait(false);

        var rng = new Randomizer(randomSeed);
        var now = DateTimeOffset.UtcNow;

        AuditEntry Build()
        {
            var path = rng.ArrayElement(Paths);
            var method = rng.ArrayElement(HttpMethods);
            var status = rng.WeightedRandom(StatusCodes, StatusWeights);
            var action = rng.ArrayElement(AllActions);
            var source = rng.ArrayElement(AllSources);
            // Distribute timestamps over the last 90 days so time-range queries have realistic cardinality.
            var timestamp = now.AddMinutes(-rng.Int(0, 60 * 24 * 90));
            return new AuditEntry
            {
                CorrelationId = Guid.NewGuid(),
                Source = source,
                Timestamp = timestamp,
                UserId = rng.Int(1, 1000)
                    .ToString(System.Globalization.CultureInfo.InvariantCulture),
                UserName = rng.Bool() ? $"user{rng.Int(1, 1000)}" : null,
                IpAddress = $"10.{rng.Byte()}.{rng.Byte()}.{rng.Byte()}",
                UserAgent = "Mozilla/5.0 perf-seeder",
                HttpMethod = method,
                Path = path,
                QueryString = rng.Bool() ? $"page={rng.Int(1, 20)}&pageSize=50" : null,
                StatusCode = status,
                DurationMs = rng.Int(1, 800),
                Module = rng.ArrayElement(Modules),
                EntityType = rng.ArrayElement(EntityTypes),
                EntityId = rng.Int(1, 10000)
                    .ToString(System.Globalization.CultureInfo.InvariantCulture),
                Action = action,
                RequestBody = null,
                Changes = null,
                Metadata = null,
            };
        }

        var sw = Stopwatch.StartNew();
        var inserted = 0L;
        var remaining = count;
        var nextReport = (long)batchSize * 10;

        await using var tx = await db.Database.BeginTransactionAsync().ConfigureAwait(false);
        while (remaining > 0)
        {
            var take = Math.Min(batchSize, remaining);
            var batch = new List<AuditEntry>(take);
            for (var i = 0; i < take; i++)
            {
                batch.Add(Build());
            }
            await db.AuditEntries.AddRangeAsync(batch).ConfigureAwait(false);
            await db.SaveChangesAsync().ConfigureAwait(false);
            db.ChangeTracker.Clear();
            inserted += take;
            remaining -= take;

            if (inserted >= nextReport || remaining == 0)
            {
                SeederProgress.Report("auditlogs", inserted, count, sw);
                nextReport = inserted + (long)batchSize * 10;
            }
        }
        await tx.CommitAsync().ConfigureAwait(false);

        SeederProgress.Final("auditlogs", inserted, sw);
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
