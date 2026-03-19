using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleModule.Host.Migrations
{
    /// <inheritdoc />
    public partial class AddPageDraftContent : Migration
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
                    table: "PageBuilder_Pages",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<string>(
                name: "DraftContent",
                table: "PageBuilder_Pages",
                type: "TEXT",
                nullable: true
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "DraftContent", table: "PageBuilder_Pages");

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
