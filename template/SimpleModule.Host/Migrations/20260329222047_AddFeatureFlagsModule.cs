using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleModule.Host.Migrations
{
    /// <inheritdoc />
    public partial class AddFeatureFlagsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FeatureFlags_FeatureFlags",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeprecated = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ConcurrencyStamp = table.Column<string>(
                        type: "TEXT",
                        maxLength: 40,
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureFlags_FeatureFlags", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "FeatureFlags_FeatureFlagOverrides",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FlagName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    OverrideType = table.Column<int>(type: "INTEGER", nullable: false),
                    OverrideValue = table.Column<string>(
                        type: "TEXT",
                        maxLength: 256,
                        nullable: false
                    ),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ConcurrencyStamp = table.Column<string>(
                        type: "TEXT",
                        maxLength: 40,
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureFlags_FeatureFlagOverrides", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlags_FeatureFlags_Name",
                table: "FeatureFlags_FeatureFlags",
                column: "Name",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlags_FeatureFlagOverrides_FlagName_OverrideType_OverrideValue",
                table: "FeatureFlags_FeatureFlagOverrides",
                columns: new[] { "FlagName", "OverrideType", "OverrideValue" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "FeatureFlags_FeatureFlagOverrides");

            migrationBuilder.DropTable(name: "FeatureFlags_FeatureFlags");
        }
    }
}
