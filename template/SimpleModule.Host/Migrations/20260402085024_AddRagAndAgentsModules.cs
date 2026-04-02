using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleModule.Host.Migrations
{
    /// <inheritdoc />
    public partial class AddRagAndAgentsModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename table from old StructuredRagCache module to new Rag module
            migrationBuilder.DropPrimaryKey(
                name: "PK_StructuredRagCache_CachedStructuredKnowledge",
                table: "StructuredRagCache_CachedStructuredKnowledge"
            );

            migrationBuilder.RenameTable(
                name: "StructuredRagCache_CachedStructuredKnowledge",
                newName: "Rag_CachedStructuredKnowledge"
            );

            migrationBuilder.RenameIndex(
                name: "IX_StructuredRagCache_CachedStructuredKnowledge_ExpiresAt",
                table: "Rag_CachedStructuredKnowledge",
                newName: "IX_Rag_CachedStructuredKnowledge_ExpiresAt"
            );

            migrationBuilder.RenameIndex(
                name: "IX_StructuredRagCache_CachedStructuredKnowledge_CollectionName_DocumentHash_StructureType",
                table: "Rag_CachedStructuredKnowledge",
                newName: "IX_Rag_CachedStructuredKnowledge_CollectionName_DocumentHash_StructureType"
            );

            migrationBuilder.AddPrimaryKey(
                name: "PK_Rag_CachedStructuredKnowledge",
                table: "Rag_CachedStructuredKnowledge",
                column: "Id"
            );

            // Agents module — session and message tables
            migrationBuilder.CreateTable(
                name: "Agents_Sessions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    AgentName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastMessageAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agents_Sessions", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Agents_Messages",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    SessionId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    TokenCount = table.Column<int>(type: "INTEGER", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agents_Messages", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Agents_Sessions_AgentName",
                table: "Agents_Sessions",
                column: "AgentName"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Agents_Sessions_UserId",
                table: "Agents_Sessions",
                column: "UserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Agents_Messages_SessionId",
                table: "Agents_Messages",
                column: "SessionId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Agents_Messages_SessionId_Timestamp",
                table: "Agents_Messages",
                columns: new[] { "SessionId", "Timestamp" }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Agents_Messages");
            migrationBuilder.DropTable(name: "Agents_Sessions");

            // Restore old table name
            migrationBuilder.DropPrimaryKey(
                name: "PK_Rag_CachedStructuredKnowledge",
                table: "Rag_CachedStructuredKnowledge"
            );

            migrationBuilder.RenameTable(
                name: "Rag_CachedStructuredKnowledge",
                newName: "StructuredRagCache_CachedStructuredKnowledge"
            );

            migrationBuilder.RenameIndex(
                name: "IX_Rag_CachedStructuredKnowledge_ExpiresAt",
                table: "StructuredRagCache_CachedStructuredKnowledge",
                newName: "IX_StructuredRagCache_CachedStructuredKnowledge_ExpiresAt"
            );

            migrationBuilder.RenameIndex(
                name: "IX_Rag_CachedStructuredKnowledge_CollectionName_DocumentHash_StructureType",
                table: "StructuredRagCache_CachedStructuredKnowledge",
                newName: "IX_StructuredRagCache_CachedStructuredKnowledge_CollectionName_DocumentHash_StructureType"
            );

            migrationBuilder.AddPrimaryKey(
                name: "PK_StructuredRagCache_CachedStructuredKnowledge",
                table: "StructuredRagCache_CachedStructuredKnowledge",
                column: "Id"
            );
        }
    }
}
