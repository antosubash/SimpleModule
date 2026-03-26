using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleModule.Host.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogDashboardIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_AuditEntries_Action",
                table: "AuditLogs_AuditEntries",
                column: "Action"
            );

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_AuditEntries_Path",
                table: "AuditLogs_AuditEntries",
                column: "Path"
            );

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_AuditEntries_Source",
                table: "AuditLogs_AuditEntries",
                column: "Source"
            );

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_AuditEntries_StatusCode",
                table: "AuditLogs_AuditEntries",
                column: "StatusCode"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_AuditEntries_Action",
                table: "AuditLogs_AuditEntries"
            );

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_AuditEntries_Path",
                table: "AuditLogs_AuditEntries"
            );

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_AuditEntries_Source",
                table: "AuditLogs_AuditEntries"
            );

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_AuditEntries_StatusCode",
                table: "AuditLogs_AuditEntries"
            );
        }
    }
}
