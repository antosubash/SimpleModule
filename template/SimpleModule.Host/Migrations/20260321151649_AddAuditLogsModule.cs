using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleModule.Host.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateTable(
                name: "AuditLogs_AuditEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", nullable: true),
                    HttpMethod = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Path = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    QueryString = table.Column<string>(type: "TEXT", nullable: true),
                    StatusCode = table.Column<int>(type: "INTEGER", nullable: true),
                    DurationMs = table.Column<long>(type: "INTEGER", nullable: true),
                    RequestBody = table.Column<string>(type: "TEXT", nullable: true),
                    Module = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    EntityId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Action = table.Column<int>(type: "INTEGER", nullable: true),
                    Changes = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata = table.Column<string>(type: "TEXT", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs_AuditEntries", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_PageBuilder_Pages_DeletedAt",
                table: "PageBuilder_Pages",
                column: "DeletedAt"
            );

            migrationBuilder.CreateIndex(
                name: "IX_PageBuilder_Pages_IsPublished",
                table: "PageBuilder_Pages",
                column: "IsPublished"
            );

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_AuditEntries_CorrelationId",
                table: "AuditLogs_AuditEntries",
                column: "CorrelationId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_AuditEntries_EntityType_EntityId",
                table: "AuditLogs_AuditEntries",
                columns: new[] { "EntityType", "EntityId" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_AuditEntries_Module_Timestamp",
                table: "AuditLogs_AuditEntries",
                columns: new[] { "Module", "Timestamp" },
                descending: new[] { false, true }
            );

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_AuditEntries_Timestamp",
                table: "AuditLogs_AuditEntries",
                column: "Timestamp",
                descending: new bool[0]
            );

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_AuditEntries_UserId_Timestamp",
                table: "AuditLogs_AuditEntries",
                columns: new[] { "UserId", "Timestamp" },
                descending: new[] { false, true }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AuditLogs_AuditEntries");

            migrationBuilder.DropIndex(
                name: "IX_PageBuilder_Pages_DeletedAt",
                table: "PageBuilder_Pages"
            );

            migrationBuilder.DropIndex(
                name: "IX_PageBuilder_Pages_IsPublished",
                table: "PageBuilder_Pages"
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
        }
    }
}
