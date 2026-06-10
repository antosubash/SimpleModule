using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Commands.Doctor.Checks;

/// <summary>
/// Detects installed packaged modules whose bundled EF migrations have not been
/// applied yet. Applied state is read from the SQLite database's
/// __EFMigrationsHistory table (the local dev default); other providers get a
/// warning that the state cannot be verified offline.
/// </summary>
public sealed partial class PendingModuleMigrationsCheck : IDoctorCheck
{
    public IEnumerable<CheckResult> Run(Infrastructure.SolutionContext solution)
    {
        // Modules with migrations: installed packages whose manifest says hasDbContext.
        var modulesWithMigrations = new List<(string PackageId, IReadOnlyList<string> Ids)>();
        foreach (
            var reference in PackageReferenceManipulator.GetPackageReferences(
                solution.ApiCsprojPath,
                solution.RootPath
            )
        )
        {
            var manifest = GlobalPackagesCache.TryReadManifest(reference.Id, reference.Version);
            if (manifest is not { HasDbContext: true })
            {
                continue;
            }

            var dll = GlobalPackagesCache.FindAssemblyPath(reference.Id, reference.Version);
            if (dll is null)
            {
                continue;
            }

            var ids = AssemblyMigrationReader.ReadMigrationIds(dll);
            if (ids.Count > 0)
            {
                modulesWithMigrations.Add((reference.Id, ids));
            }
        }

        if (modulesWithMigrations.Count == 0)
        {
            yield break;
        }

        var dbPath = FindSqliteDatabasePath(solution);
        if (dbPath is null || !File.Exists(dbPath))
        {
            yield return new CheckResult(
                "Module migrations",
                CheckStatus.Warning,
                $"{modulesWithMigrations.Count} module(s) bundle migrations but the database "
                    + "could not be inspected offline (non-SQLite or not yet created) — they "
                    + "apply on next start or 'SIMPLEMODULE_MIGRATE_ONLY=1 dotnet run'"
            );
            yield break;
        }

        var applied = ReadAppliedMigrations(dbPath);
        if (applied is null)
        {
            yield return new CheckResult(
                "Module migrations",
                CheckStatus.Warning,
                "the database could not be read (locked or recovering) — migration state unverified"
            );
            yield break;
        }

        foreach (var (packageId, ids) in modulesWithMigrations)
        {
            var pending = ids.Where(id => !applied.Contains(id)).ToList();
            yield return pending.Count == 0
                ? new CheckResult(
                    $"Migrations {packageId}",
                    CheckStatus.Pass,
                    $"{ids.Count} migration(s) applied"
                )
                : new CheckResult(
                    $"Migrations {packageId}",
                    CheckStatus.Fail,
                    $"{pending.Count} pending migration(s) ({string.Join(", ", pending.Take(3))}"
                        + (pending.Count > 3 ? ", …" : "")
                        + ") — run 'SIMPLEMODULE_MIGRATE_ONLY=1 dotnet run --project <host>'"
                );
        }
    }

    private static string? FindSqliteDatabasePath(Infrastructure.SolutionContext solution)
    {
        var hostDir = Path.GetDirectoryName(solution.ApiCsprojPath)!;
        foreach (var settingsFile in new[] { "appsettings.Development.json", "appsettings.json" })
        {
            var path = Path.Combine(hostDir, settingsFile);
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                if (
                    doc.RootElement.TryGetProperty("Database", out var db)
                    && db.TryGetProperty("DefaultConnection", out var conn)
                    && conn.GetString() is { } connectionString
                )
                {
                    var match = DataSourceRegex().Match(connectionString);
                    if (match.Success)
                    {
                        var dataSource = match.Groups["path"].Value.Trim();
                        return Path.IsPathRooted(dataSource)
                            ? dataSource
                            : Path.Combine(hostDir, dataSource);
                    }
                }
            }
            catch (JsonException)
            {
                // malformed settings — fall through
            }
        }

        return null;
    }

    private static HashSet<string>? ReadAppliedMigrations(string dbPath)
    {
        var applied = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            using var connection = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT MigrationId FROM __EFMigrationsHistory";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                applied.Add(reader.GetString(0));
            }
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 1)
        {
            // "no such table": EnsureCreated database — everything is pending.
        }
        catch (SqliteException)
        {
            // Locked/busy/corrupt: unknown state, NOT "all pending".
            return null;
        }

        return applied;
    }

    [GeneratedRegex("Data Source=(?<path>[^;]+)", RegexOptions.IgnoreCase)]
    private static partial Regex DataSourceRegex();
}
