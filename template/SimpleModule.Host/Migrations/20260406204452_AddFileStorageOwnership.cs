using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleModule.Host.Migrations
{
    /// <inheritdoc />
    public partial class AddFileStorageOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "FileStorage_StoredFiles",
                type: "TEXT",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileStorage_StoredFiles_CreatedByUserId",
                table: "FileStorage_StoredFiles",
                column: "CreatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FileStorage_StoredFiles_CreatedByUserId",
                table: "FileStorage_StoredFiles");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "FileStorage_StoredFiles");
        }
    }
}
