using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleModule.Host.Migrations
{
    /// <inheritdoc />
    public partial class AddPageEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "PageBuilder_Pages",
                type: "TEXT",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "MetaDescription",
                table: "PageBuilder_Pages",
                type: "TEXT",
                maxLength: 300,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "MetaKeywords",
                table: "PageBuilder_Pages",
                type: "TEXT",
                maxLength: 500,
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "OgImage",
                table: "PageBuilder_Pages",
                type: "TEXT",
                maxLength: 500,
                nullable: true
            );

            migrationBuilder.CreateTable(
                name: "PageBuilder_Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PageId = table.Column<int>(type: "INTEGER", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageBuilder_Tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PageBuilder_Tags_PageBuilder_Pages_PageId",
                        column: x => x.PageId,
                        principalTable: "PageBuilder_Pages",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "PageBuilder_Templates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageBuilder_Templates", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_PageBuilder_Tags_Name",
                table: "PageBuilder_Tags",
                column: "Name",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_PageBuilder_Tags_PageId",
                table: "PageBuilder_Tags",
                column: "PageId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_PageBuilder_Templates_Name",
                table: "PageBuilder_Templates",
                column: "Name",
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PageBuilder_Tags");

            migrationBuilder.DropTable(name: "PageBuilder_Templates");

            migrationBuilder.DropColumn(name: "DeletedAt", table: "PageBuilder_Pages");

            migrationBuilder.DropColumn(name: "MetaDescription", table: "PageBuilder_Pages");

            migrationBuilder.DropColumn(name: "MetaKeywords", table: "PageBuilder_Pages");

            migrationBuilder.DropColumn(name: "OgImage", table: "PageBuilder_Pages");
        }
    }
}
