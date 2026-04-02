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
            migrationBuilder.CreateTable(
                name: "Rag_CachedStructuredKnowledge",
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
                    table.PrimaryKey("PK_Rag_CachedStructuredKnowledge", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Rag_CachedStructuredKnowledge_CollectionName_DocumentHash_StructureType",
                table: "Rag_CachedStructuredKnowledge",
                columns: new[] { "CollectionName", "DocumentHash", "StructureType" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Rag_CachedStructuredKnowledge_ExpiresAt",
                table: "Rag_CachedStructuredKnowledge",
                column: "ExpiresAt"
            );

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
            migrationBuilder.DropTable(name: "Rag_CachedStructuredKnowledge");
        }
    }
}
