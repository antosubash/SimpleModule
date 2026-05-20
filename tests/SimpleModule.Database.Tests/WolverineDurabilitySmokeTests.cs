using JasperFx;
using JasperFx.Resources;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimpleModule.Core.Events;
using Wolverine;
using Wolverine.Sqlite;

namespace SimpleModule.Database.Tests;

/// <summary>
/// Smoke-tests that the durable Wolverine wiring used by the framework actually
/// persists envelopes to the configured database before dispatch. If Wolverine
/// ever regressed to in-memory transport these assertions would fail.
/// </summary>
public sealed class WolverineDurabilitySmokeTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(
        Path.GetTempPath(),
        $"wolverine-smoke-{Guid.NewGuid():N}.db"
    );

    private string ConnectionString => $"Data Source={_dbPath}";

    public void Dispose()
    {
        try
        {
            File.Delete(_dbPath);
        }
#pragma warning disable CA1031
        catch
        {
            // Best-effort cleanup; SQLite may still hold the file briefly.
        }
#pragma warning restore CA1031
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Wolverine_Creates_Outbox_Schema_On_Startup()
    {
        using var host = await BuildHostAsync();

        var tables = await ReadTableNamesAsync();
        tables.Should().Contain(t => t.StartsWith("wolverine_", StringComparison.Ordinal));

        await host.StopAsync();
    }

    [Fact]
    public async Task PublishAsync_Persists_Envelope_To_Outbox_Before_Dispatch()
    {
        SlowEventHandler.Reset();

        using var host = await BuildHostAsync();
        var bus = host.Services.GetRequiredService<IMessageBus>();

        // Publish without awaiting the handler — the durable outbox guarantees the
        // envelope row exists after PublishAsync completes, even before the handler
        // finishes.
        await bus.PublishAsync(new SlowEvent());

        var outgoingCount = await CountWolverineEnvelopesAsync();
        outgoingCount
            .Should()
            .BeGreaterThan(
                0,
                "publishing must write the envelope to a Wolverine durability table"
            );

        // Let the handler drain so the host shuts down cleanly.
        SlowEventHandler.Completion.SetResult();
        await SlowEventHandler.InvokedTask.Task;

        await host.StopAsync();
    }

    private async Task<IHost> BuildHostAsync()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.UseWolverine(opts =>
        {
            opts.Discovery.IncludeAssembly(typeof(WolverineDurabilitySmokeTests).Assembly);
            opts.PersistMessagesWithSqlite(ConnectionString);
            opts.Policies.UseDurableLocalQueues();
        });

        builder.Services.AddResourceSetupOnStartup();

        var host = builder.Build();
        await host.StartAsync();
        return host;
    }

    private Task<IReadOnlyList<string>> ReadTableNamesAsync() =>
        ReadTableNamesAsync(ConnectionString);

    private Task<long> CountWolverineEnvelopesAsync() =>
        CountWolverineEnvelopesAsync(ConnectionString);

    private static async Task<IReadOnlyList<string>> ReadTableNamesAsync(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name";

        var names = new List<string>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }
        return names;
    }

    private static async Task<long> CountWolverineEnvelopesAsync(string connectionString)
    {
        var tables = await ReadTableNamesAsync(connectionString);
        var envelopeTables = tables
            .Where(t => t.StartsWith("wolverine_", StringComparison.Ordinal))
            .Where(t => t.Contains("envelope", StringComparison.OrdinalIgnoreCase))
            .ToList();

        long total = 0;
        using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        foreach (var table in envelopeTables)
        {
            using var command = connection.CreateCommand();
            // Table names sourced from sqlite_master (not user input); safe to interpolate.
#pragma warning disable CA2100
            command.CommandText = $"SELECT COUNT(*) FROM \"{table}\"";
#pragma warning restore CA2100
            var result = await command.ExecuteScalarAsync();
            if (result is long count)
            {
                total += count;
            }
        }
        return total;
    }
}

public sealed record SlowEvent : DomainEvent;

public static class SlowEventHandler
{
    public static TaskCompletionSource Completion { get; private set; } = new();
    public static TaskCompletionSource InvokedTask { get; private set; } = new();

    public static void Reset()
    {
        Completion = new TaskCompletionSource();
        InvokedTask = new TaskCompletionSource();
    }

    public static async Task Handle(SlowEvent _)
    {
        InvokedTask.TrySetResult();
        await Completion.Task;
    }
}
