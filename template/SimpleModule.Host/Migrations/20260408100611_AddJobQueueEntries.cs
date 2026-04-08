using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleModule.Host.Migrations
{
    /// <inheritdoc />
    public partial class AddJobQueueEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackgroundJobs_CronTickerOccurrences");

            migrationBuilder.DropTable(
                name: "BackgroundJobs_TimeTickers");

            migrationBuilder.DropTable(
                name: "Users_AspNetUserPasskeys");

            migrationBuilder.DropTable(
                name: "BackgroundJobs_CronTickers");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Tenants_Tenants",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Tenants_TenantHosts",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "RateLimiting_Rules",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Products_Products",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "PageBuilder_Templates",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "PageBuilder_Tags",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "PageBuilder_Pages",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Orders_Orders",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "FileStorage_StoredFiles",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Email_EmailTemplates",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Email_EmailMessages",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "AuditLogs_AuditEntries",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.CreateTable(
                name: "BackgroundJobs_JobQueueEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    JobTypeName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    SerializedData = table.Column<string>(type: "TEXT", nullable: true),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    ClaimedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ClaimedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    AttemptCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Error = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CronExpression = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RecurringName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundJobs_JobQueueEntries", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Products_Products",
                keyColumn: "Id",
                keyValue: 1,
                column: "Price",
                value: 99168m);

            migrationBuilder.UpdateData(
                table: "Products_Products",
                keyColumn: "Id",
                keyValue: 2,
                column: "Price",
                value: 44622m);

            migrationBuilder.UpdateData(
                table: "Products_Products",
                keyColumn: "Id",
                keyValue: 3,
                column: "Price",
                value: 66012m);

            migrationBuilder.UpdateData(
                table: "Products_Products",
                keyColumn: "Id",
                keyValue: 4,
                column: "Price",
                value: 63367m);

            migrationBuilder.UpdateData(
                table: "Products_Products",
                keyColumn: "Id",
                keyValue: 5,
                column: "Price",
                value: 67430m);

            migrationBuilder.UpdateData(
                table: "Products_Products",
                keyColumn: "Id",
                keyValue: 6,
                column: "Price",
                value: 85163m);

            migrationBuilder.UpdateData(
                table: "Products_Products",
                keyColumn: "Id",
                keyValue: 7,
                column: "Price",
                value: 41748m);

            migrationBuilder.UpdateData(
                table: "Products_Products",
                keyColumn: "Id",
                keyValue: 8,
                column: "Price",
                value: 97556m);

            migrationBuilder.UpdateData(
                table: "Products_Products",
                keyColumn: "Id",
                keyValue: 9,
                column: "Price",
                value: 92809m);

            migrationBuilder.UpdateData(
                table: "Products_Products",
                keyColumn: "Id",
                keyValue: 10,
                column: "Price",
                value: 59260m);

            migrationBuilder.CreateIndex(
                name: "IX_JobQueueEntries_RecurringName",
                table: "BackgroundJobs_JobQueueEntries",
                column: "RecurringName");

            migrationBuilder.CreateIndex(
                name: "IX_JobQueueEntries_State_ScheduledAt",
                table: "BackgroundJobs_JobQueueEntries",
                columns: new[] { "State", "ScheduledAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackgroundJobs_JobQueueEntries");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Tenants_Tenants",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Tenants_TenantHosts",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "RateLimiting_Rules",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Products_Products",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "PageBuilder_Templates",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "PageBuilder_Tags",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "PageBuilder_Pages",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Orders_Orders",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "FileStorage_StoredFiles",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Email_EmailTemplates",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Email_EmailMessages",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "AuditLogs_AuditEntries",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.CreateTable(
                name: "BackgroundJobs_CronTickers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Expression = table.Column<string>(type: "TEXT", nullable: true),
                    Function = table.Column<string>(type: "TEXT", nullable: true),
                    InitIdentifier = table.Column<string>(type: "TEXT", nullable: true),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Request = table.Column<byte[]>(type: "BLOB", nullable: true),
                    Retries = table.Column<int>(type: "INTEGER", nullable: false),
                    RetryIntervals = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundJobs_CronTickers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BackgroundJobs_TimeTickers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ElapsedTime = table.Column<long>(type: "INTEGER", nullable: false),
                    ExceptionMessage = table.Column<string>(type: "TEXT", nullable: true),
                    ExecutedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExecutionTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Function = table.Column<string>(type: "TEXT", nullable: true),
                    InitIdentifier = table.Column<string>(type: "TEXT", nullable: true),
                    LockHolder = table.Column<string>(type: "TEXT", nullable: true),
                    LockedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Request = table.Column<byte[]>(type: "BLOB", nullable: true),
                    Retries = table.Column<int>(type: "INTEGER", nullable: false),
                    RetryCount = table.Column<int>(type: "INTEGER", nullable: false),
                    RetryIntervals = table.Column<string>(type: "TEXT", nullable: true),
                    RunCondition = table.Column<int>(type: "INTEGER", nullable: true),
                    SkippedReason = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundJobs_TimeTickers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BackgroundJobs_TimeTickers_BackgroundJobs_TimeTickers_ParentId",
                        column: x => x.ParentId,
                        principalTable: "BackgroundJobs_TimeTickers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Users_AspNetUserPasskeys",
                columns: table => new
                {
                    CredentialId = table.Column<byte[]>(type: "BLOB", maxLength: 1024, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Data = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users_AspNetUserPasskeys", x => x.CredentialId);
                    table.ForeignKey(
                        name: "FK_Users_AspNetUserPasskeys_Users_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "Users_AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BackgroundJobs_CronTickerOccurrences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CronTickerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ElapsedTime = table.Column<long>(type: "INTEGER", nullable: false),
                    ExceptionMessage = table.Column<string>(type: "TEXT", nullable: true),
                    ExecutedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExecutionTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LockHolder = table.Column<string>(type: "TEXT", nullable: true),
                    LockedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RetryCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SkippedReason = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundJobs_CronTickerOccurrences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BackgroundJobs_CronTickerOccurrences_BackgroundJobs_CronTickers_CronTickerId",
                        column: x => x.CronTickerId,
                        principalTable: "BackgroundJobs_CronTickers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Products_Products",
                keyColumn: "Id",
                keyValue: 1,
                column: "Price",
                value: 991.68m);

            migrationBuilder.UpdateData(
                table: "Products_Products",
                keyColumn: "Id",
                keyValue: 2,
                column: "Price",
                value: 446.22m);

            migrationBuilder.UpdateData(
                table: "Products_Products",
                keyColumn: "Id",
                keyValue: 3,
                column: "Price",
                value: 660.12m);

            migrationBuilder.UpdateData(
                table: "Products_Products",
                keyColumn: "Id",
                keyValue: 4,
                column: "Price",
                value: 633.67m);

            migrationBuilder.UpdateData(
                table: "Products_Products",
                keyColumn: "Id",
                keyValue: 5,
                column: "Price",
                value: 674.30m);

            migrationBuilder.UpdateData(
                table: "Products_Products",
                keyColumn: "Id",
                keyValue: 6,
                column: "Price",
                value: 851.63m);

            migrationBuilder.UpdateData(
                table: "Products_Products",
                keyColumn: "Id",
                keyValue: 7,
                column: "Price",
                value: 417.48m);

            migrationBuilder.UpdateData(
                table: "Products_Products",
                keyColumn: "Id",
                keyValue: 8,
                column: "Price",
                value: 975.56m);

            migrationBuilder.UpdateData(
                table: "Products_Products",
                keyColumn: "Id",
                keyValue: 9,
                column: "Price",
                value: 928.09m);

            migrationBuilder.UpdateData(
                table: "Products_Products",
                keyColumn: "Id",
                keyValue: 10,
                column: "Price",
                value: 592.60m);

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobs_CronTickerOccurrences_CronTickerId",
                table: "BackgroundJobs_CronTickerOccurrences",
                column: "CronTickerId");

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobs_TimeTickers_ParentId",
                table: "BackgroundJobs_TimeTickers",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_AspNetUserPasskeys_UserId",
                table: "Users_AspNetUserPasskeys",
                column: "UserId");
        }
    }
}
