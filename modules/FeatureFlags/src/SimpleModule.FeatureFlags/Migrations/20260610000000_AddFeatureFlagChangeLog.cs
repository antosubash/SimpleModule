using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SimpleModule.FeatureFlags.Migrations;

/// <summary>
/// Reference example of a module-bundled EF migration (the packaging contract:
/// packaged modules ship their schema changes as migrations — see
/// docs/site/advanced/module-packaging.md). Applied automatically at host
/// startup and by `sm add`/`sm upgrade` migrate-only runs.
/// Raw idempotent SQL because module tables may also have been created by the
/// unified HostDbContext's EnsureCreated on fresh development databases.
/// </summary>
[DbContext(typeof(FeatureFlagsDbContext))]
[Migration("20260610000000_AddFeatureFlagChangeLog")]
public sealed class AddFeatureFlagChangeLog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS "FeatureFlagChangeLog" (
                "Id" TEXT NOT NULL PRIMARY KEY,
                "FlagKey" TEXT NOT NULL,
                "ChangedBy" TEXT NOT NULL,
                "OldValue" TEXT NULL,
                "NewValue" TEXT NULL,
                "ChangedAtUtc" TEXT NOT NULL
            );
            """
        );
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""DROP TABLE IF EXISTS "FeatureFlagChangeLog";""");
    }
}
