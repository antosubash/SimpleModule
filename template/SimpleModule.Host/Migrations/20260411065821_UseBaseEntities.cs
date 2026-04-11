using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleModule.Host.Migrations
{
    /// <inheritdoc />
    public partial class UseBaseEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PageBuilder_Pages_DeletedAt",
                table: "PageBuilder_Pages"
            );

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "Tenants_Tenants",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "Tenants_TenantHosts",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "Settings_Settings",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "Settings_Settings",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Settings_Settings",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(
                    new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    new TimeSpan(0, 0, 0, 0, 0)
                )
            );

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "Settings_PublicMenuItems",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "Settings_PublicMenuItems",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "RateLimiting_Rules",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(
                    new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    new TimeSpan(0, 0, 0, 0, 0)
                ),
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true
            );

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "RateLimiting_Rules",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "RateLimiting_Rules",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "Products_Products",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "Products_Products",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Products_Products",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(
                    new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    new TimeSpan(0, 0, 0, 0, 0)
                )
            );

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Products_Products",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(
                    new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    new TimeSpan(0, 0, 0, 0, 0)
                )
            );

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "PageBuilder_Templates",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "PageBuilder_Templates",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "PageBuilder_Templates",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(
                    new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    new TimeSpan(0, 0, 0, 0, 0)
                )
            );

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "PageBuilder_Tags",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "PageBuilder_Pages",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "PageBuilder_Pages",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "PageBuilder_Pages",
                type: "TEXT",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "PageBuilder_Pages",
                type: "TEXT",
                nullable: true
            );

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PageBuilder_Pages",
                type: "INTEGER",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "PageBuilder_Pages",
                type: "TEXT",
                nullable: true
            );

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "PageBuilder_Pages",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "Orders_Orders",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "Orders_Orders",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Orders_Orders",
                type: "TEXT",
                nullable: true
            );

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Orders_Orders",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(
                    new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    new TimeSpan(0, 0, 0, 0, 0)
                )
            );

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Orders_Orders",
                type: "TEXT",
                nullable: true
            );

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "FileStorage_StoredFiles",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "FileStorage_StoredFiles",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "FileStorage_StoredFiles",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(
                    new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    new TimeSpan(0, 0, 0, 0, 0)
                )
            );

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "FeatureFlags_FeatureFlags",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "FeatureFlags_FeatureFlagOverrides",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Email_EmailTemplates",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(
                    new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    new TimeSpan(0, 0, 0, 0, 0)
                ),
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true
            );

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "Email_EmailTemplates",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "Email_EmailTemplates",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "Email_EmailMessages",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "Email_EmailMessages",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Email_EmailMessages",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(
                    new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    new TimeSpan(0, 0, 0, 0, 0)
                )
            );

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "Chat_Conversations",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "Chat_ChatMessages",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<long>(
                name: "UpdatedAt",
                table: "Chat_ChatMessages",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L
            );

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "BackgroundJobs_JobQueueEntries",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "BackgroundJobs_JobQueueEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(
                    new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    new TimeSpan(0, 0, 0, 0, 0)
                )
            );

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "BackgroundJobs_JobProgress",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "BackgroundJobs_JobProgress",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(
                    new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    new TimeSpan(0, 0, 0, 0, 0)
                )
            );

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "AuditLogs_AuditEntries",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "Agents_Sessions",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Agents_Sessions",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(
                    new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    new TimeSpan(0, 0, 0, 0, 0)
                )
            );

            migrationBuilder.UpdateData(
                table: "Products_Products",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "UpdatedAt" },
                values: new object[]
                {
                    "",
                    new DateTimeOffset(
                        new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                        new TimeSpan(0, 0, 0, 0, 0)
                    ),
                    new DateTimeOffset(
                        new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                        new TimeSpan(0, 0, 0, 0, 0)
                    ),
                }
            );

            migrationBuilder.UpdateData(
                table: "Products_Products",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "UpdatedAt" },
                values: new object[]
                {
                    "",
                    new DateTimeOffset(
                        new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                        new TimeSpan(0, 0, 0, 0, 0)
                    ),
                    new DateTimeOffset(
                        new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                        new TimeSpan(0, 0, 0, 0, 0)
                    ),
                }
            );

            migrationBuilder.UpdateData(
                table: "Products_Products",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "UpdatedAt" },
                values: new object[]
                {
                    "",
                    new DateTimeOffset(
                        new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                        new TimeSpan(0, 0, 0, 0, 0)
                    ),
                    new DateTimeOffset(
                        new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                        new TimeSpan(0, 0, 0, 0, 0)
                    ),
                }
            );

            migrationBuilder.UpdateData(
                table: "Products_Products",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "UpdatedAt" },
                values: new object[]
                {
                    "",
                    new DateTimeOffset(
                        new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                        new TimeSpan(0, 0, 0, 0, 0)
                    ),
                    new DateTimeOffset(
                        new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                        new TimeSpan(0, 0, 0, 0, 0)
                    ),
                }
            );

            migrationBuilder.UpdateData(
                table: "Products_Products",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "UpdatedAt" },
                values: new object[]
                {
                    "",
                    new DateTimeOffset(
                        new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                        new TimeSpan(0, 0, 0, 0, 0)
                    ),
                    new DateTimeOffset(
                        new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                        new TimeSpan(0, 0, 0, 0, 0)
                    ),
                }
            );

            migrationBuilder.UpdateData(
                table: "Products_Products",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "UpdatedAt" },
                values: new object[]
                {
                    "",
                    new DateTimeOffset(
                        new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                        new TimeSpan(0, 0, 0, 0, 0)
                    ),
                    new DateTimeOffset(
                        new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                        new TimeSpan(0, 0, 0, 0, 0)
                    ),
                }
            );

            migrationBuilder.UpdateData(
                table: "Products_Products",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "UpdatedAt" },
                values: new object[]
                {
                    "",
                    new DateTimeOffset(
                        new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                        new TimeSpan(0, 0, 0, 0, 0)
                    ),
                    new DateTimeOffset(
                        new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                        new TimeSpan(0, 0, 0, 0, 0)
                    ),
                }
            );

            migrationBuilder.UpdateData(
                table: "Products_Products",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "UpdatedAt" },
                values: new object[]
                {
                    "",
                    new DateTimeOffset(
                        new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                        new TimeSpan(0, 0, 0, 0, 0)
                    ),
                    new DateTimeOffset(
                        new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                        new TimeSpan(0, 0, 0, 0, 0)
                    ),
                }
            );

            migrationBuilder.UpdateData(
                table: "Products_Products",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "UpdatedAt" },
                values: new object[]
                {
                    "",
                    new DateTimeOffset(
                        new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                        new TimeSpan(0, 0, 0, 0, 0)
                    ),
                    new DateTimeOffset(
                        new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                        new TimeSpan(0, 0, 0, 0, 0)
                    ),
                }
            );

            migrationBuilder.UpdateData(
                table: "Products_Products",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "UpdatedAt" },
                values: new object[]
                {
                    "",
                    new DateTimeOffset(
                        new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                        new TimeSpan(0, 0, 0, 0, 0)
                    ),
                    new DateTimeOffset(
                        new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                        new TimeSpan(0, 0, 0, 0, 0)
                    ),
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_PageBuilder_Pages_IsDeleted_DeletedAt",
                table: "PageBuilder_Pages",
                columns: new[] { "IsDeleted", "DeletedAt" }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PageBuilder_Pages_IsDeleted_DeletedAt",
                table: "PageBuilder_Pages"
            );

            migrationBuilder.DropColumn(name: "ConcurrencyStamp", table: "Settings_Settings");

            migrationBuilder.DropColumn(name: "CreatedAt", table: "Settings_Settings");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "Settings_PublicMenuItems"
            );

            migrationBuilder.DropColumn(name: "ConcurrencyStamp", table: "RateLimiting_Rules");

            migrationBuilder.DropColumn(name: "ConcurrencyStamp", table: "Products_Products");

            migrationBuilder.DropColumn(name: "CreatedAt", table: "Products_Products");

            migrationBuilder.DropColumn(name: "UpdatedAt", table: "Products_Products");

            migrationBuilder.DropColumn(name: "ConcurrencyStamp", table: "PageBuilder_Templates");

            migrationBuilder.DropColumn(name: "UpdatedAt", table: "PageBuilder_Templates");

            migrationBuilder.DropColumn(name: "ConcurrencyStamp", table: "PageBuilder_Pages");

            migrationBuilder.DropColumn(name: "CreatedBy", table: "PageBuilder_Pages");

            migrationBuilder.DropColumn(name: "DeletedBy", table: "PageBuilder_Pages");

            migrationBuilder.DropColumn(name: "IsDeleted", table: "PageBuilder_Pages");

            migrationBuilder.DropColumn(name: "UpdatedBy", table: "PageBuilder_Pages");

            migrationBuilder.DropColumn(name: "Version", table: "PageBuilder_Pages");

            migrationBuilder.DropColumn(name: "ConcurrencyStamp", table: "Orders_Orders");

            migrationBuilder.DropColumn(name: "CreatedBy", table: "Orders_Orders");

            migrationBuilder.DropColumn(name: "UpdatedAt", table: "Orders_Orders");

            migrationBuilder.DropColumn(name: "UpdatedBy", table: "Orders_Orders");

            migrationBuilder.DropColumn(name: "ConcurrencyStamp", table: "FileStorage_StoredFiles");

            migrationBuilder.DropColumn(name: "UpdatedAt", table: "FileStorage_StoredFiles");

            migrationBuilder.DropColumn(name: "ConcurrencyStamp", table: "Email_EmailTemplates");

            migrationBuilder.DropColumn(name: "ConcurrencyStamp", table: "Email_EmailMessages");

            migrationBuilder.DropColumn(name: "UpdatedAt", table: "Email_EmailMessages");

            migrationBuilder.DropColumn(name: "ConcurrencyStamp", table: "Chat_Conversations");

            migrationBuilder.DropColumn(name: "ConcurrencyStamp", table: "Chat_ChatMessages");

            migrationBuilder.DropColumn(name: "UpdatedAt", table: "Chat_ChatMessages");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "BackgroundJobs_JobQueueEntries"
            );

            migrationBuilder.DropColumn(name: "UpdatedAt", table: "BackgroundJobs_JobQueueEntries");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "BackgroundJobs_JobProgress"
            );

            migrationBuilder.DropColumn(name: "CreatedAt", table: "BackgroundJobs_JobProgress");

            migrationBuilder.DropColumn(name: "ConcurrencyStamp", table: "Agents_Sessions");

            migrationBuilder.DropColumn(name: "UpdatedAt", table: "Agents_Sessions");

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "Tenants_Tenants",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "Tenants_TenantHosts",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "Settings_Settings",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "Settings_PublicMenuItems",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "RateLimiting_Rules",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT"
            );

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "RateLimiting_Rules",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "Products_Products",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "PageBuilder_Templates",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "PageBuilder_Tags",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "PageBuilder_Pages",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "Orders_Orders",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "FileStorage_StoredFiles",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "FeatureFlags_FeatureFlags",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "FeatureFlags_FeatureFlagOverrides",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Email_EmailTemplates",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT"
            );

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "Email_EmailTemplates",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "Email_EmailMessages",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "AuditLogs_AuditEntries",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.CreateIndex(
                name: "IX_PageBuilder_Pages_DeletedAt",
                table: "PageBuilder_Pages",
                column: "DeletedAt"
            );
        }
    }
}
