using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleModule.Host.Migrations
{
    /// <inheritdoc />
    public partial class AddPasskeySupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    table: "RateLimiting_Rules",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .OldAnnotation("Sqlite:Autoincrement", true);

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

            migrationBuilder.CreateTable(
                name: "Users_AspNetUserPasskeys",
                columns: table => new
                {
                    CredentialId = table.Column<byte[]>(
                        type: "BLOB",
                        maxLength: 1024,
                        nullable: false
                    ),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Data = table.Column<string>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users_AspNetUserPasskeys", x => x.CredentialId);
                    table.ForeignKey(
                        name: "FK_Users_AspNetUserPasskeys_Users_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "Users_AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Users_AspNetUserPasskeys_UserId",
                table: "Users_AspNetUserPasskeys",
                column: "UserId"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Users_AspNetUserPasskeys");

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
        }
    }
}
