using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Npgsql;
using SimpleModule.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.Jobs;

public sealed class JobsListScheduledCommand : Command<JobsListScheduledSettings>
{
    public override int Execute(CommandContext context, JobsListScheduledSettings settings)
    {
        var (connection, provider) = ResolveConnection(settings);
        if (connection is null)
        {
            AnsiConsole.MarkupLine(
                "[red]No database connection. Pass --connection or run from a project with appsettings.json.[/]"
            );
            return 1;
        }

        try
        {
            using var conn = OpenConnection(provider, connection);
            var rows = ReadSchedules(conn).ToList();
            if (rows.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No scheduled jobs found.[/]");
                return 0;
            }

            var table = new Table().RoundedBorder();
            table.AddColumn("Name");
            table.AddColumn("Job type");
            table.AddColumn("Cron");
            table.AddColumn("TZ");
            table.AddColumn("Next run");
            table.AddColumn("Last run");
            table.AddColumn("Flags");

            foreach (var row in rows.OrderBy(r => r.NextRunAt ?? DateTimeOffset.MaxValue))
            {
                var flags = new List<string>();
                if (!row.IsEnabled)
                    flags.Add("[red]disabled[/]");
                if (row.WithoutOverlapping)
                    flags.Add("mutex");
                if (row.OnOneServer)
                    flags.Add("single");

                table.AddRow(
                    Markup.Escape(row.Name),
                    Markup.Escape(ShortType(row.JobTypeName)),
                    Markup.Escape(row.CronExpression),
                    Markup.Escape(row.TimeZoneId),
                    FormatTimestamp(row.NextRunAt),
                    FormatTimestamp(row.LastRunAt),
                    string.Join(", ", flags)
                );
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"\n[dim]{rows.Count} scheduled job(s)[/]");
            return 0;
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            AnsiConsole.MarkupLine($"[red]Failed to read scheduled jobs:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }

    private static (string? Connection, string Provider) ResolveConnection(
        JobsListScheduledSettings settings
    )
    {
        if (!string.IsNullOrWhiteSpace(settings.ConnectionString))
        {
            return (settings.ConnectionString, settings.Provider ?? GuessProvider(settings.ConnectionString));
        }

        var solution = SolutionContext.Discover();
        if (solution is null)
            return (null, "Sqlite");

        var path = Path.Combine(solution.RootPath, "template", "SimpleModule.Host", "appsettings.json");
        if (!File.Exists(path))
        {
            var hostDir = Directory
                .EnumerateFiles(solution.RootPath, "appsettings.json", SearchOption.AllDirectories)
                .FirstOrDefault();
            if (hostDir is null)
                return (null, "Sqlite");
            path = hostDir;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        if (!doc.RootElement.TryGetProperty("Database", out var db))
            return (null, "Sqlite");

        var conn = db.TryGetProperty("DefaultConnection", out var cs) ? cs.GetString() : null;
        var provider = db.TryGetProperty("Provider", out var p) ? p.GetString() : "Sqlite";
        return (conn, provider ?? "Sqlite");
    }

    private static string GuessProvider(string connection) =>
        connection.Contains("Data Source", StringComparison.OrdinalIgnoreCase) ? "Sqlite" : "Postgres";

    private static DbConnection OpenConnection(string provider, string connectionString)
    {
        DbConnection conn = provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase)
            ? new NpgsqlConnection(connectionString)
            : new SqliteConnection(connectionString);
        conn.Open();
        return conn;
    }

    private static IEnumerable<ScheduleRow> ReadSchedules(DbConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT "Name", "JobTypeName", "CronExpression", "TimeZoneId",
                   "IsEnabled", "WithoutOverlapping", "OnOneServer",
                   "LastRunAt", "NextRunAt"
            FROM "ScheduledJobStates"
            """;
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            yield return new ScheduleRow(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetBoolean(4),
                reader.GetBoolean(5),
                reader.GetBoolean(6),
                ReadTimestamp(reader, 7),
                ReadTimestamp(reader, 8)
            );
        }
    }

    private static DateTimeOffset? ReadTimestamp(DbDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
            return null;
        var raw = reader.GetValue(ordinal);
        return raw switch
        {
            DateTimeOffset dto => dto,
            DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)),
            string s when DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null,
        };
    }

    private static string FormatTimestamp(DateTimeOffset? value) =>
        value is null
            ? "[dim]—[/]"
            : Markup.Escape(value.Value.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture));

    private static string ShortType(string assemblyQualifiedName)
    {
        var comma = assemblyQualifiedName.IndexOf(',', StringComparison.Ordinal);
        var fullName = comma < 0 ? assemblyQualifiedName : assemblyQualifiedName[..comma].Trim();
        var lastDot = fullName.LastIndexOf('.');
        return lastDot < 0 ? fullName : fullName[(lastDot + 1)..];
    }

    private sealed record ScheduleRow(
        string Name,
        string JobTypeName,
        string CronExpression,
        string TimeZoneId,
        bool IsEnabled,
        bool WithoutOverlapping,
        bool OnOneServer,
        DateTimeOffset? LastRunAt,
        DateTimeOffset? NextRunAt
    );
}
