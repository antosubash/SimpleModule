using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SimpleModule.Host.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Agents_Messages");

            migrationBuilder.DropTable(name: "Agents_Sessions");

            migrationBuilder.DropTable(name: "Chat_ChatMessages");

            migrationBuilder.DropTable(name: "Datasets_Datasets");

            migrationBuilder.DropTable(name: "Map_Basemaps");

            migrationBuilder.DropTable(name: "Map_LayerSources");

            migrationBuilder.DropTable(name: "Map_MapBasemap");

            migrationBuilder.DropTable(name: "Map_MapLayer");

            migrationBuilder.DropTable(name: "Orders_OrderItems");

            migrationBuilder.DropTable(name: "PageBuilder_Tags");

            migrationBuilder.DropTable(name: "PageBuilder_Templates");

            migrationBuilder.DropTable(name: "Products_Products");

            migrationBuilder.DropTable(name: "Rag_CachedStructuredKnowledge");

            migrationBuilder.DropTable(name: "Chat_Conversations");

            migrationBuilder.DropTable(name: "Map_SavedMaps");

            migrationBuilder.DropTable(name: "Orders_Orders");

            migrationBuilder.DropTable(name: "PageBuilder_Pages");

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
                    table: "Settings_Settings",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "Settings_PublicMenuItems",
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
                    table: "FeatureFlags_FeatureFlags",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "FeatureFlags_FeatureFlagOverrides",
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
                name: "Notifications_Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Channel = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Body = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    DataJson = table.Column<string>(type: "TEXT", nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ConcurrencyStamp = table.Column<string>(
                        type: "TEXT",
                        maxLength: 64,
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications_Notifications", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Notifications_UserId_CreatedAt_Id",
                table: "Notifications_Notifications",
                columns: new[] { "UserId", "CreatedAt", "Id" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Notifications_UserId_ReadAt",
                table: "Notifications_Notifications",
                columns: new[] { "UserId", "ReadAt" }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Notifications_Notifications");

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
                    table: "Settings_Settings",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "Settings_PublicMenuItems",
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
                    table: "FeatureFlags_FeatureFlags",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "FeatureFlags_FeatureFlagOverrides",
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

            migrationBuilder.CreateTable(
                name: "Agents_Messages",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SessionId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    TokenCount = table.Column<int>(type: "INTEGER", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agents_Messages", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Agents_Sessions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    AgentName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ConcurrencyStamp = table.Column<string>(
                        type: "TEXT",
                        maxLength: 64,
                        nullable: false
                    ),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastMessageAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agents_Sessions", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Chat_Conversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AgentName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ConcurrencyStamp = table.Column<string>(
                        type: "TEXT",
                        maxLength: 64,
                        nullable: false
                    ),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    Pinned = table.Column<bool>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chat_Conversations", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Datasets_Datasets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BboxMaxX = table.Column<double>(type: "REAL", nullable: true),
                    BboxMaxY = table.Column<double>(type: "REAL", nullable: true),
                    BboxMinX = table.Column<double>(type: "REAL", nullable: true),
                    BboxMinY = table.Column<double>(type: "REAL", nullable: true),
                    ConcurrencyStamp = table.Column<string>(
                        type: "TEXT",
                        maxLength: 64,
                        nullable: false
                    ),
                    ContentHash = table.Column<string>(
                        type: "TEXT",
                        maxLength: 128,
                        nullable: true
                    ),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(
                        type: "TEXT",
                        maxLength: 4096,
                        nullable: true
                    ),
                    FeatureCount = table.Column<long>(type: "INTEGER", nullable: true),
                    Format = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    MetadataJson = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    NormalizedPath = table.Column<string>(
                        type: "TEXT",
                        maxLength: 1024,
                        nullable: true
                    ),
                    OriginalFileName = table.Column<string>(
                        type: "TEXT",
                        maxLength: 512,
                        nullable: false
                    ),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    SourceSrid = table.Column<int>(type: "INTEGER", nullable: true),
                    Srid = table.Column<int>(type: "INTEGER", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    StoragePath = table.Column<string>(
                        type: "TEXT",
                        maxLength: 1024,
                        nullable: false
                    ),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Datasets_Datasets", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Map_Basemaps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Attribution = table.Column<string>(
                        type: "TEXT",
                        maxLength: 500,
                        nullable: true
                    ),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(
                        type: "TEXT",
                        maxLength: 2000,
                        nullable: true
                    ),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    StyleUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    ThumbnailUrl = table.Column<string>(
                        type: "TEXT",
                        maxLength: 2048,
                        nullable: true
                    ),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Map_Basemaps", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Map_LayerSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Attribution = table.Column<string>(
                        type: "TEXT",
                        maxLength: 500,
                        nullable: true
                    ),
                    Bounds = table.Column<string>(type: "TEXT", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(
                        type: "TEXT",
                        maxLength: 2000,
                        nullable: true
                    ),
                    MaxZoom = table.Column<int>(type: "INTEGER", nullable: true),
                    Metadata = table.Column<string>(type: "TEXT", nullable: false),
                    MinZoom = table.Column<int>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Url = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Map_LayerSources", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Map_SavedMaps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BaseStyleUrl = table.Column<string>(
                        type: "TEXT",
                        maxLength: 2048,
                        nullable: false
                    ),
                    Bearing = table.Column<double>(type: "REAL", nullable: false),
                    CenterLat = table.Column<double>(type: "REAL", nullable: false),
                    CenterLng = table.Column<double>(type: "REAL", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(
                        type: "TEXT",
                        maxLength: 2000,
                        nullable: true
                    ),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Pitch = table.Column<double>(type: "REAL", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Zoom = table.Column<double>(type: "REAL", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Map_SavedMaps", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Orders_Orders",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConcurrencyStamp = table.Column<string>(
                        type: "TEXT",
                        maxLength: 64,
                        nullable: false
                    ),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders_Orders", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "PageBuilder_Pages",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConcurrencyStamp = table.Column<string>(
                        type: "TEXT",
                        maxLength: 64,
                        nullable: false
                    ),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    DraftContent = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPublished = table.Column<bool>(
                        type: "INTEGER",
                        nullable: false,
                        defaultValue: false
                    ),
                    MetaDescription = table.Column<string>(
                        type: "TEXT",
                        maxLength: 300,
                        nullable: true
                    ),
                    MetaKeywords = table.Column<string>(
                        type: "TEXT",
                        maxLength: 500,
                        nullable: true
                    ),
                    OgImage = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Order = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageBuilder_Pages", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "PageBuilder_Templates",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConcurrencyStamp = table.Column<string>(
                        type: "TEXT",
                        maxLength: 64,
                        nullable: false
                    ),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageBuilder_Templates", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Products_Products",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConcurrencyStamp = table.Column<string>(
                        type: "TEXT",
                        maxLength: 64,
                        nullable: false
                    ),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products_Products", x => x.Id);
                }
            );

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
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DocumentHash = table.Column<string>(
                        type: "TEXT",
                        maxLength: 64,
                        nullable: false
                    ),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    HitCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceTitle = table.Column<string>(
                        type: "TEXT",
                        maxLength: 512,
                        nullable: false
                    ),
                    StructureType = table.Column<int>(type: "INTEGER", nullable: false),
                    StructuredContent = table.Column<string>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rag_CachedStructuredKnowledge", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Chat_ChatMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ConcurrencyStamp = table.Column<string>(
                        type: "TEXT",
                        maxLength: 64,
                        nullable: false
                    ),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    ConversationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chat_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Chat_ChatMessages_Chat_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Chat_Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Map_MapBasemap",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BasemapId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    SavedMapId = table.Column<Guid>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Map_MapBasemap", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Map_MapBasemap_Map_SavedMaps_SavedMapId",
                        column: x => x.SavedMapId,
                        principalTable: "Map_SavedMaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Map_MapLayer",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LayerSourceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Opacity = table.Column<double>(type: "REAL", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    SavedMapId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StyleOverrides = table.Column<string>(type: "TEXT", nullable: false),
                    Visible = table.Column<bool>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Map_MapLayer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Map_MapLayer_Map_SavedMaps_SavedMapId",
                        column: x => x.SavedMapId,
                        principalTable: "Map_SavedMaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Orders_OrderItems",
                columns: table => new
                {
                    OrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders_OrderItems", x => new { x.OrderId, x.ProductId });
                    table.ForeignKey(
                        name: "FK_Orders_OrderItems_Orders_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders_Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "PageBuilder_Tags",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
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

            migrationBuilder.InsertData(
                table: "Map_Basemaps",
                columns: new[]
                {
                    "Id",
                    "Attribution",
                    "ConcurrencyStamp",
                    "CreatedAt",
                    "CreatedBy",
                    "Description",
                    "Name",
                    "StyleUrl",
                    "ThumbnailUrl",
                    "UpdatedAt",
                    "UpdatedBy",
                },
                values: new object[,]
                {
                    {
                        new Guid("22222222-2222-2222-2222-000000000001"),
                        "MapLibre",
                        "seed-basemap-demotiles",
                        new DateTimeOffset(
                            new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        null,
                        "Official MapLibre demo vector style. Free for development.",
                        "MapLibre Demotiles",
                        "https://demotiles.maplibre.org/style.json",
                        null,
                        new DateTimeOffset(
                            new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        null,
                    },
                    {
                        new Guid("22222222-2222-2222-2222-000000000002"),
                        "© OpenStreetMap contributors, OpenFreeMap",
                        "seed-basemap-openfreemap-liberty",
                        new DateTimeOffset(
                            new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        null,
                        "OpenFreeMap free vector basemap, Liberty style.",
                        "OpenFreeMap Liberty",
                        "https://tiles.openfreemap.org/styles/liberty",
                        null,
                        new DateTimeOffset(
                            new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        null,
                    },
                    {
                        new Guid("22222222-2222-2222-2222-000000000003"),
                        "© OpenStreetMap contributors, OpenFreeMap",
                        "seed-basemap-openfreemap-positron",
                        new DateTimeOffset(
                            new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        null,
                        "OpenFreeMap free vector basemap, light Positron style.",
                        "OpenFreeMap Positron",
                        "https://tiles.openfreemap.org/styles/positron",
                        null,
                        new DateTimeOffset(
                            new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        null,
                    },
                    {
                        new Guid("22222222-2222-2222-2222-000000000004"),
                        "© OpenStreetMap contributors, OpenFreeMap",
                        "seed-basemap-openfreemap-bright",
                        new DateTimeOffset(
                            new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        null,
                        "OpenFreeMap free vector basemap, Bright style.",
                        "OpenFreeMap Bright",
                        "https://tiles.openfreemap.org/styles/bright",
                        null,
                        new DateTimeOffset(
                            new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        null,
                    },
                    {
                        new Guid("22222222-2222-2222-2222-000000000005"),
                        "© OpenStreetMap contributors, VersaTiles",
                        "seed-basemap-versatiles-colorful",
                        new DateTimeOffset(
                            new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        null,
                        "VersaTiles free OSM-based vector basemap, Colorful style.",
                        "Versatiles Colorful",
                        "https://tiles.versatiles.org/assets/styles/colorful/style.json",
                        null,
                        new DateTimeOffset(
                            new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        null,
                    },
                }
            );

            migrationBuilder.InsertData(
                table: "Map_LayerSources",
                columns: new[]
                {
                    "Id",
                    "Attribution",
                    "Bounds",
                    "ConcurrencyStamp",
                    "CreatedAt",
                    "CreatedBy",
                    "Description",
                    "MaxZoom",
                    "Metadata",
                    "MinZoom",
                    "Name",
                    "Type",
                    "UpdatedAt",
                    "UpdatedBy",
                    "Url",
                },
                values: new object[,]
                {
                    {
                        new Guid("11111111-1111-1111-1111-000000000001"),
                        "© OpenStreetMap contributors",
                        "[-180,-85,180,85]",
                        "seed-osm-xyz",
                        new DateTimeOffset(
                            new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        null,
                        "Standard OSM raster tiles. Free for low-volume use; respect the OSMF tile usage policy.",
                        19,
                        "{}",
                        0,
                        "OpenStreetMap (raster tiles)",
                        3,
                        new DateTimeOffset(
                            new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        null,
                        "https://tile.openstreetmap.org/{z}/{x}/{y}.png",
                    },
                    {
                        new Guid("11111111-1111-1111-1111-000000000002"),
                        "© OpenStreetMap contributors, terrestris",
                        "[-180,-85,180,85]",
                        "seed-terrestris-wms",
                        new DateTimeOffset(
                            new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        null,
                        "Public WMS by terrestris. Used in the official MapLibre 'Add a WMS source' example.",
                        null,
                        "{\"layers\":\"OSM-WMS\",\"format\":\"image/png\",\"version\":\"1.1.1\",\"crs\":\"EPSG:3857\",\"transparent\":\"true\"}",
                        null,
                        "terrestris OSM-WMS",
                        0,
                        new DateTimeOffset(
                            new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        null,
                        "https://ows.terrestris.de/osm/service",
                    },
                    {
                        new Guid("11111111-1111-1111-1111-000000000003"),
                        "© OpenStreetMap contributors, terrestris",
                        "[-180,-85,180,85]",
                        "seed-terrestris-topo",
                        new DateTimeOffset(
                            new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        null,
                        "terrestris topographic WMS overlay layer (transparent).",
                        null,
                        "{\"layers\":\"TOPO-WMS,OSM-Overlay-WMS\",\"format\":\"image/png\",\"version\":\"1.1.1\",\"crs\":\"EPSG:3857\",\"transparent\":\"true\"}",
                        null,
                        "terrestris TOPO-WMS",
                        0,
                        new DateTimeOffset(
                            new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        null,
                        "https://ows.terrestris.de/osm/service",
                    },
                    {
                        new Guid("11111111-1111-1111-1111-000000000004"),
                        "MapLibre",
                        "[-180,-85,180,85]",
                        "seed-maplibre-demotiles",
                        new DateTimeOffset(
                            new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        null,
                        "Official MapLibre demo MVT vector tileset. Free for development.",
                        14,
                        "{\"sourceLayer\":\"countries\"}",
                        0,
                        "MapLibre demotiles (vector)",
                        4,
                        new DateTimeOffset(
                            new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        null,
                        "https://demotiles.maplibre.org/tiles/{z}/{x}/{y}.pbf",
                    },
                    {
                        new Guid("11111111-1111-1111-1111-000000000005"),
                        "© OpenStreetMap contributors, Protomaps",
                        "[11.154,43.727,11.328,43.823]",
                        "seed-protomaps-firenze",
                        new DateTimeOffset(
                            new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        null,
                        "Public PMTiles vector archive of Florence (ODbL). Used in the MapLibre PMTiles example.",
                        null,
                        "{\"tileType\":\"vector\",\"sourceLayer\":\"landuse\"}",
                        null,
                        "Protomaps Firenze (PMTiles)",
                        5,
                        new DateTimeOffset(
                            new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        null,
                        "https://pmtiles.io/protomaps(vector)ODbL_firenze.pmtiles",
                    },
                    {
                        new Guid("11111111-1111-1111-1111-000000000006"),
                        "Geomatico",
                        "[-180,-85,180,85]",
                        "seed-geomatico-cog",
                        new DateTimeOffset(
                            new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        null,
                        "Public Cloud-Optimized GeoTIFF demo from the maplibre-cog-protocol sample viewer.",
                        null,
                        "{}",
                        null,
                        "Geomatico kriging COG (demo)",
                        6,
                        new DateTimeOffset(
                            new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        null,
                        "https://labs.geomatico.es/maplibre-cog-protocol/data/kriging.tif",
                    },
                    {
                        new Guid("11111111-1111-1111-1111-000000000007"),
                        "USGS / MapLibre demo",
                        "[-180,-85,180,85]",
                        "seed-maplibre-earthquakes",
                        new DateTimeOffset(
                            new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        null,
                        "Small public GeoJSON FeatureCollection from the MapLibre demo assets.",
                        null,
                        "{\"color\":\"#ef4444\"}",
                        null,
                        "MapLibre demotiles point sample (GeoJSON)",
                        7,
                        new DateTimeOffset(
                            new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        null,
                        "https://maplibre.org/maplibre-gl-js/docs/assets/significant-earthquakes-2015.geojson",
                    },
                }
            );

            migrationBuilder.InsertData(
                table: "Products_Products",
                columns: new[]
                {
                    "Id",
                    "ConcurrencyStamp",
                    "CreatedAt",
                    "Name",
                    "Price",
                    "UpdatedAt",
                },
                values: new object[,]
                {
                    {
                        1,
                        "",
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        "Fantastic Rubber Shoes",
                        991.68m,
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                    },
                    {
                        2,
                        "",
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        "Fantastic Rubber Bacon",
                        446.22m,
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                    },
                    {
                        3,
                        "",
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        "Fantastic Concrete Bike",
                        660.12m,
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                    },
                    {
                        4,
                        "",
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        "Handcrafted Concrete Keyboard",
                        633.67m,
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                    },
                    {
                        5,
                        "",
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        "Intelligent Frozen Mouse",
                        674.30m,
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                    },
                    {
                        6,
                        "",
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        "Sleek Soft Hat",
                        851.63m,
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                    },
                    {
                        7,
                        "",
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        "Practical Fresh Bike",
                        417.48m,
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                    },
                    {
                        8,
                        "",
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        "Handmade Steel Ball",
                        975.56m,
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                    },
                    {
                        9,
                        "",
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        "Ergonomic Fresh Pants",
                        928.09m,
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                    },
                    {
                        10,
                        "",
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        "Licensed Steel Sausages",
                        592.60m,
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                    },
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
                name: "IX_Chat_ChatMessages_ConversationId_CreatedAt",
                table: "Chat_ChatMessages",
                columns: new[] { "ConversationId", "CreatedAt" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Chat_Conversations_UserId_UpdatedAt",
                table: "Chat_Conversations",
                columns: new[] { "UserId", "UpdatedAt" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Datasets_Datasets_BboxMinX_BboxMaxX_BboxMinY_BboxMaxY",
                table: "Datasets_Datasets",
                columns: new[] { "BboxMinX", "BboxMaxX", "BboxMinY", "BboxMaxY" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Datasets_Datasets_ContentHash",
                table: "Datasets_Datasets",
                column: "ContentHash"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Datasets_Datasets_Format",
                table: "Datasets_Datasets",
                column: "Format"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Datasets_Datasets_IsDeleted_CreatedAt",
                table: "Datasets_Datasets",
                columns: new[] { "IsDeleted", "CreatedAt" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Datasets_Datasets_Status",
                table: "Datasets_Datasets",
                column: "Status"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Map_MapBasemap_SavedMapId",
                table: "Map_MapBasemap",
                column: "SavedMapId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Map_MapLayer_SavedMapId",
                table: "Map_MapLayer",
                column: "SavedMapId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_PageBuilder_Pages_IsDeleted_DeletedAt",
                table: "PageBuilder_Pages",
                columns: new[] { "IsDeleted", "DeletedAt" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_PageBuilder_Pages_IsPublished",
                table: "PageBuilder_Pages",
                column: "IsPublished"
            );

            migrationBuilder.CreateIndex(
                name: "IX_PageBuilder_Pages_Slug",
                table: "PageBuilder_Pages",
                column: "Slug",
                unique: true
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
        }
    }
}
