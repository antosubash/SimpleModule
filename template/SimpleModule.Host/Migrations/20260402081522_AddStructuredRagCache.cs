using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleModule.Host.Migrations
{
    /// <inheritdoc />
    public partial class AddStructuredRagCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StructuredRagCache_CachedStructuredKnowledge",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CollectionName = table.Column<string>(
                        type: "TEXT",
                        maxLength: 256,
                        nullable: false
                    ),
                    DocumentHash = table.Column<string>(
                        type: "TEXT",
                        maxLength: 64,
                        nullable: false
                    ),
                    StructureType = table.Column<int>(type: "INTEGER", nullable: false),
                    StructuredContent = table.Column<string>(type: "TEXT", nullable: false),
                    SourceTitle = table.Column<string>(
                        type: "TEXT",
                        maxLength: 512,
                        nullable: false
                    ),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    HitCount = table.Column<int>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StructuredRagCache_CachedStructuredKnowledge", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_StructuredRagCache_CachedStructuredKnowledge_CollectionName_DocumentHash_StructureType",
                table: "StructuredRagCache_CachedStructuredKnowledge",
                columns: new[] { "CollectionName", "DocumentHash", "StructureType" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_StructuredRagCache_CachedStructuredKnowledge_ExpiresAt",
                table: "StructuredRagCache_CachedStructuredKnowledge",
                column: "ExpiresAt"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "StructuredRagCache_CachedStructuredKnowledge");
        }
    }
}
