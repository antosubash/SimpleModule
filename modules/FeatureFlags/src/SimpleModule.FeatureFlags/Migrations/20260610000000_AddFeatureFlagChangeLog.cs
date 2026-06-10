using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SimpleModule.FeatureFlags.Migrations;

/// <summary>
/// Reference example of a module-bundled EF migration (the packaging contract:
/// packaged modules ship their schema changes as migrations — see
/// docs/site/advanced/module-packaging.md). Applied automatically at host
/// startup and by `sm add`/`sm upgrade` migrate-only runs.
/// Module migrations must respect the schema-per-module convention
/// (ApplyModuleSchema): a "{Module}_" table prefix on SQLite, a lowercase
/// module schema on other providers. Raw idempotent SQL because module tables
/// may also have been created by the unified HostDbContext's EnsureCreated on
/// fresh development databases.
/// </summary>
[DbContext(typeof(FeatureFlagsDbContext))]
[Migration("20260610000000_AddFeatureFlagChangeLog")]
public sealed class AddFeatureFlagChangeLog : Migration
{
    private const string Columns = """
            "Id" TEXT NOT NULL PRIMARY KEY,
            "FlagKey" TEXT NOT NULL,
            "ChangedBy" TEXT NOT NULL,
            "OldValue" TEXT NULL,
            "NewValue" TEXT NULL,
            "ChangedAtUtc" TEXT NOT NULL
        """;

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        if (migrationBuilder.IsSqlite())
        {
            migrationBuilder.Sql(
                $"""CREATE TABLE IF NOT EXISTS "FeatureFlags_FeatureFlagChangeLog" ({Columns});"""
            );
        }
        else
        {
            migrationBuilder.Sql("""CREATE SCHEMA IF NOT EXISTS "featureflags";""");
            migrationBuilder.Sql(
                $"""CREATE TABLE IF NOT EXISTS "featureflags"."FeatureFlagChangeLog" ({Columns});"""
            );
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        if (migrationBuilder.IsSqlite())
        {
            migrationBuilder.Sql("""DROP TABLE IF EXISTS "FeatureFlags_FeatureFlagChangeLog";""");
        }
        else
        {
            migrationBuilder.Sql("""DROP TABLE IF EXISTS "featureflags"."FeatureFlagChangeLog";""");
        }
    }
}
