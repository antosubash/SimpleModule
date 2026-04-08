using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SimpleModule.Host.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "AuditLogs_AuditEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", nullable: true),
                    HttpMethod = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Path = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    QueryString = table.Column<string>(type: "TEXT", nullable: true),
                    StatusCode = table.Column<int>(type: "INTEGER", nullable: true),
                    DurationMs = table.Column<long>(type: "INTEGER", nullable: true),
                    RequestBody = table.Column<string>(type: "TEXT", nullable: true),
                    Module = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    EntityId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Action = table.Column<int>(type: "INTEGER", nullable: true),
                    Changes = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata = table.Column<string>(type: "TEXT", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs_AuditEntries", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "BackgroundJobs_JobProgress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    JobTypeName = table.Column<string>(
                        type: "TEXT",
                        maxLength: 500,
                        nullable: false
                    ),
                    ModuleName = table.Column<string>(
                        type: "TEXT",
                        maxLength: 100,
                        nullable: false
                    ),
                    ProgressPercentage = table.Column<int>(type: "INTEGER", nullable: false),
                    ProgressMessage = table.Column<string>(
                        type: "TEXT",
                        maxLength: 1000,
                        nullable: true
                    ),
                    Data = table.Column<string>(type: "TEXT", nullable: true),
                    Logs = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundJobs_JobProgress", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "BackgroundJobs_JobQueueEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    JobTypeName = table.Column<string>(
                        type: "TEXT",
                        maxLength: 500,
                        nullable: false
                    ),
                    SerializedData = table.Column<string>(type: "TEXT", nullable: true),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    ClaimedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ClaimedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    AttemptCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Error = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CronExpression = table.Column<string>(
                        type: "TEXT",
                        maxLength: 100,
                        nullable: true
                    ),
                    RecurringName = table.Column<string>(
                        type: "TEXT",
                        maxLength: 200,
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundJobs_JobQueueEntries", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Chat_Conversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    AgentName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Pinned = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false),
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
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    OriginalFileName = table.Column<string>(
                        type: "TEXT",
                        maxLength: 512,
                        nullable: false
                    ),
                    ContentHash = table.Column<string>(
                        type: "TEXT",
                        maxLength: 128,
                        nullable: true
                    ),
                    Format = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceSrid = table.Column<int>(type: "INTEGER", nullable: true),
                    Srid = table.Column<int>(type: "INTEGER", nullable: true),
                    BboxMinX = table.Column<double>(type: "REAL", nullable: true),
                    BboxMinY = table.Column<double>(type: "REAL", nullable: true),
                    BboxMaxX = table.Column<double>(type: "REAL", nullable: true),
                    BboxMaxY = table.Column<double>(type: "REAL", nullable: true),
                    FeatureCount = table.Column<long>(type: "INTEGER", nullable: true),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    StoragePath = table.Column<string>(
                        type: "TEXT",
                        maxLength: 1024,
                        nullable: false
                    ),
                    NormalizedPath = table.Column<string>(
                        type: "TEXT",
                        maxLength: 1024,
                        nullable: true
                    ),
                    MetadataJson = table.Column<string>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(
                        type: "TEXT",
                        maxLength: 4096,
                        nullable: true
                    ),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ConcurrencyStamp = table.Column<string>(
                        type: "TEXT",
                        maxLength: 64,
                        nullable: false
                    ),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Datasets_Datasets", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Email_EmailMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    To = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Cc = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Bcc = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ReplyTo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    IsHtml = table.Column<bool>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ErrorMessage = table.Column<string>(
                        type: "TEXT",
                        maxLength: 2000,
                        nullable: true
                    ),
                    RetryCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TemplateSlug = table.Column<string>(
                        type: "TEXT",
                        maxLength: 200,
                        nullable: true
                    ),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SentAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Email_EmailMessages", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Email_EmailTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    IsHtml = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultReplyTo = table.Column<string>(
                        type: "TEXT",
                        maxLength: 500,
                        nullable: true
                    ),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Email_EmailTemplates", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "FeatureFlags_FeatureFlagOverrides",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FlagName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    OverrideType = table.Column<int>(type: "INTEGER", nullable: false),
                    OverrideValue = table.Column<string>(
                        type: "TEXT",
                        maxLength: 256,
                        nullable: false
                    ),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ConcurrencyStamp = table.Column<string>(
                        type: "TEXT",
                        maxLength: 40,
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureFlags_FeatureFlagOverrides", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "FeatureFlags_FeatureFlags",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeprecated = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ConcurrencyStamp = table.Column<string>(
                        type: "TEXT",
                        maxLength: 40,
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureFlags_FeatureFlags", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "FileStorage_StoredFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    StoragePath = table.Column<string>(
                        type: "TEXT",
                        maxLength: 1024,
                        nullable: false
                    ),
                    ContentType = table.Column<string>(
                        type: "TEXT",
                        maxLength: 256,
                        nullable: false
                    ),
                    Size = table.Column<long>(type: "INTEGER", nullable: false),
                    Folder = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    CreatedByUserId = table.Column<string>(
                        type: "TEXT",
                        maxLength: 450,
                        nullable: true
                    ),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileStorage_StoredFiles", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Map_Basemaps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(
                        type: "TEXT",
                        maxLength: 2000,
                        nullable: true
                    ),
                    StyleUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    Attribution = table.Column<string>(
                        type: "TEXT",
                        maxLength: 500,
                        nullable: true
                    ),
                    ThumbnailUrl = table.Column<string>(
                        type: "TEXT",
                        maxLength: 2048,
                        nullable: true
                    ),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
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
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(
                        type: "TEXT",
                        maxLength: 2000,
                        nullable: true
                    ),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    Attribution = table.Column<string>(
                        type: "TEXT",
                        maxLength: 500,
                        nullable: true
                    ),
                    MinZoom = table.Column<int>(type: "INTEGER", nullable: true),
                    MaxZoom = table.Column<int>(type: "INTEGER", nullable: true),
                    Bounds = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
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
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(
                        type: "TEXT",
                        maxLength: 2000,
                        nullable: true
                    ),
                    CenterLng = table.Column<double>(type: "REAL", nullable: false),
                    CenterLat = table.Column<double>(type: "REAL", nullable: false),
                    Zoom = table.Column<double>(type: "REAL", nullable: false),
                    Pitch = table.Column<double>(type: "REAL", nullable: false),
                    Bearing = table.Column<double>(type: "REAL", nullable: false),
                    BaseStyleUrl = table.Column<string>(
                        type: "TEXT",
                        maxLength: 2048,
                        nullable: false
                    ),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Map_SavedMaps", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "OpenIddict_OpenIddictApplications",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ApplicationType = table.Column<string>(
                        type: "TEXT",
                        maxLength: 50,
                        nullable: true
                    ),
                    ClientId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ClientSecret = table.Column<string>(type: "TEXT", nullable: true),
                    ClientType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ConcurrencyToken = table.Column<string>(
                        type: "TEXT",
                        maxLength: 50,
                        nullable: true
                    ),
                    ConsentType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    DisplayNames = table.Column<string>(type: "TEXT", nullable: true),
                    JsonWebKeySet = table.Column<string>(type: "TEXT", nullable: true),
                    Permissions = table.Column<string>(type: "TEXT", nullable: true),
                    PostLogoutRedirectUris = table.Column<string>(type: "TEXT", nullable: true),
                    Properties = table.Column<string>(type: "TEXT", nullable: true),
                    RedirectUris = table.Column<string>(type: "TEXT", nullable: true),
                    Requirements = table.Column<string>(type: "TEXT", nullable: true),
                    Settings = table.Column<string>(type: "TEXT", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenIddict_OpenIddictApplications", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "OpenIddict_OpenIddictScopes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ConcurrencyToken = table.Column<string>(
                        type: "TEXT",
                        maxLength: 50,
                        nullable: true
                    ),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Descriptions = table.Column<string>(type: "TEXT", nullable: true),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    DisplayNames = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Properties = table.Column<string>(type: "TEXT", nullable: true),
                    Resources = table.Column<string>(type: "TEXT", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenIddict_OpenIddictScopes", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Orders_Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
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
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    DraftContent = table.Column<string>(type: "TEXT", nullable: true),
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
                    IsPublished = table.Column<bool>(
                        type: "INTEGER",
                        nullable: false,
                        defaultValue: false
                    ),
                    Order = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
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

            migrationBuilder.CreateTable(
                name: "Permissions_RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<string>(type: "TEXT", nullable: false),
                    Permission = table.Column<string>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_Permissions_RolePermissions",
                        x => new { x.RoleId, x.Permission }
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Permissions_UserPermissions",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Permission = table.Column<string>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_Permissions_UserPermissions",
                        x => new { x.UserId, x.Permission }
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Products_Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "RateLimiting_Rules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    PolicyName = table.Column<string>(
                        type: "TEXT",
                        maxLength: 100,
                        nullable: false
                    ),
                    PolicyType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Target = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PermitLimit = table.Column<int>(type: "INTEGER", nullable: false),
                    WindowSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    SegmentsPerWindow = table.Column<int>(type: "INTEGER", nullable: false),
                    TokenLimit = table.Column<int>(type: "INTEGER", nullable: false),
                    TokensPerPeriod = table.Column<int>(type: "INTEGER", nullable: false),
                    ReplenishmentPeriodSeconds = table.Column<int>(
                        type: "INTEGER",
                        nullable: false
                    ),
                    QueueLimit = table.Column<int>(type: "INTEGER", nullable: false),
                    EndpointPattern = table.Column<string>(
                        type: "TEXT",
                        maxLength: 500,
                        nullable: true
                    ),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RateLimiting_Rules", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Settings_PublicMenuItems",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ParentId = table.Column<int>(type: "INTEGER", nullable: true),
                    Label = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    PageRoute = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    CssClass = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    OpenInNewTab = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsVisible = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsHomePage = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings_PublicMenuItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Settings_PublicMenuItems_Settings_PublicMenuItems_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Settings_PublicMenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Settings_Settings",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true),
                    Scope = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings_Settings", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Tenants_Tenants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    EditionName = table.Column<string>(
                        type: "TEXT",
                        maxLength: 128,
                        nullable: true
                    ),
                    ConnectionString = table.Column<string>(
                        type: "TEXT",
                        maxLength: 1024,
                        nullable: true
                    ),
                    ValidUpTo = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants_Tenants", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Users_AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(
                        type: "TEXT",
                        maxLength: 256,
                        nullable: true
                    ),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users_AspNetRoles", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Users_AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeactivatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(
                        type: "TEXT",
                        maxLength: 256,
                        nullable: true
                    ),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(
                        type: "TEXT",
                        maxLength: 256,
                        nullable: true
                    ),
                    EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                    SecurityStamp = table.Column<string>(type: "TEXT", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumber = table.Column<string>(
                        type: "TEXT",
                        maxLength: 256,
                        nullable: true
                    ),
                    PhoneNumberConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users_AspNetUsers", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Chat_ChatMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ConversationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
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
                name: "Map_SavedMapBasemaps",
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
                    table.PrimaryKey("PK_Map_SavedMapBasemaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Map_SavedMapBasemaps_Map_SavedMaps_SavedMapId",
                        column: x => x.SavedMapId,
                        principalTable: "Map_SavedMaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Map_SavedMapLayers",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LayerSourceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Visible = table.Column<bool>(type: "INTEGER", nullable: false),
                    Opacity = table.Column<double>(type: "REAL", nullable: false),
                    StyleOverrides = table.Column<string>(type: "TEXT", nullable: false),
                    SavedMapId = table.Column<Guid>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Map_SavedMapLayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Map_SavedMapLayers_Map_SavedMaps_SavedMapId",
                        column: x => x.SavedMapId,
                        principalTable: "Map_SavedMaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "OpenIddict_OpenIddictAuthorizations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ApplicationId = table.Column<string>(type: "TEXT", nullable: true),
                    ConcurrencyToken = table.Column<string>(
                        type: "TEXT",
                        maxLength: 50,
                        nullable: true
                    ),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Properties = table.Column<string>(type: "TEXT", nullable: true),
                    Scopes = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenIddict_OpenIddictAuthorizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpenIddict_OpenIddictAuthorizations_OpenIddict_OpenIddictApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "OpenIddict_OpenIddictApplications",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Orders_OrderItems",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    OrderId = table.Column<int>(type: "INTEGER", nullable: false),
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
                name: "Tenants_TenantHosts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    HostName = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants_TenantHosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tenants_TenantHosts_Tenants_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Users_AspNetRoleClaims",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_AspNetRoleClaims_Users_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Users_AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Users_AspNetUserClaims",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_AspNetUserClaims_Users_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "Users_AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Users_AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(
                        type: "TEXT",
                        maxLength: 128,
                        nullable: false
                    ),
                    ProviderKey = table.Column<string>(
                        type: "TEXT",
                        maxLength: 128,
                        nullable: false
                    ),
                    ProviderDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_Users_AspNetUserLogins",
                        x => new { x.LoginProvider, x.ProviderKey }
                    );
                    table.ForeignKey(
                        name: "FK_Users_AspNetUserLogins_Users_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "Users_AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

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

            migrationBuilder.CreateTable(
                name: "Users_AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_Users_AspNetUserRoles_Users_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Users_AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_Users_AspNetUserRoles_Users_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "Users_AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Users_AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    LoginProvider = table.Column<string>(
                        type: "TEXT",
                        maxLength: 128,
                        nullable: false
                    ),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_Users_AspNetUserTokens",
                        x => new
                        {
                            x.UserId,
                            x.LoginProvider,
                            x.Name,
                        }
                    );
                    table.ForeignKey(
                        name: "FK_Users_AspNetUserTokens_Users_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "Users_AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "OpenIddict_OpenIddictTokens",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ApplicationId = table.Column<string>(type: "TEXT", nullable: true),
                    AuthorizationId = table.Column<string>(type: "TEXT", nullable: true),
                    ConcurrencyToken = table.Column<string>(
                        type: "TEXT",
                        maxLength: 50,
                        nullable: true
                    ),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExpirationDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Payload = table.Column<string>(type: "TEXT", nullable: true),
                    Properties = table.Column<string>(type: "TEXT", nullable: true),
                    RedemptionDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReferenceId = table.Column<string>(
                        type: "TEXT",
                        maxLength: 100,
                        nullable: true
                    ),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenIddict_OpenIddictTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpenIddict_OpenIddictTokens_OpenIddict_OpenIddictApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "OpenIddict_OpenIddictApplications",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_OpenIddict_OpenIddictTokens_OpenIddict_OpenIddictAuthorizations_AuthorizationId",
                        column: x => x.AuthorizationId,
                        principalTable: "OpenIddict_OpenIddictAuthorizations",
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
                columns: new[] { "Id", "Name", "Price" },
                values: new object[,]
                {
                    { 1, "Fantastic Rubber Shoes", 991.68m },
                    { 2, "Fantastic Rubber Bacon", 446.22m },
                    { 3, "Fantastic Concrete Bike", 660.12m },
                    { 4, "Handcrafted Concrete Keyboard", 633.67m },
                    { 5, "Intelligent Frozen Mouse", 674.30m },
                    { 6, "Sleek Soft Hat", 851.63m },
                    { 7, "Practical Fresh Bike", 417.48m },
                    { 8, "Handmade Steel Ball", 975.56m },
                    { 9, "Ergonomic Fresh Pants", 928.09m },
                    { 10, "Licensed Steel Sausages", 592.60m },
                }
            );

            migrationBuilder.InsertData(
                table: "Tenants_Tenants",
                columns: new[]
                {
                    "Id",
                    "AdminEmail",
                    "ConcurrencyStamp",
                    "ConnectionString",
                    "CreatedAt",
                    "CreatedBy",
                    "EditionName",
                    "Name",
                    "Slug",
                    "Status",
                    "UpdatedAt",
                    "UpdatedBy",
                    "ValidUpTo",
                },
                values: new object[,]
                {
                    {
                        1,
                        "admin@acme.com",
                        "seed-acme",
                        null,
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        null,
                        "Enterprise",
                        "Acme Corporation",
                        "acme",
                        0,
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        null,
                        null,
                    },
                    {
                        2,
                        "admin@contoso.com",
                        "seed-contoso",
                        null,
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        null,
                        "Standard",
                        "Contoso Ltd",
                        "contoso",
                        0,
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        null,
                        null,
                    },
                    {
                        3,
                        "admin@suspended.com",
                        "seed-suspended",
                        null,
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        null,
                        null,
                        "Suspended Corp",
                        "suspended-corp",
                        1,
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        null,
                        null,
                    },
                }
            );

            migrationBuilder.InsertData(
                table: "Tenants_TenantHosts",
                columns: new[]
                {
                    "Id",
                    "ConcurrencyStamp",
                    "CreatedAt",
                    "HostName",
                    "IsActive",
                    "TenantId",
                    "UpdatedAt",
                },
                values: new object[,]
                {
                    {
                        1,
                        "seed-host-1",
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        "acme.localhost",
                        true,
                        1,
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                    },
                    {
                        2,
                        "seed-host-2",
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        "acme.local",
                        true,
                        1,
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                    },
                    {
                        3,
                        "seed-host-3",
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        "contoso.localhost",
                        true,
                        2,
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
                name: "IX_AuditLogs_AuditEntries_Action",
                table: "AuditLogs_AuditEntries",
                column: "Action"
            );

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_AuditEntries_CorrelationId",
                table: "AuditLogs_AuditEntries",
                column: "CorrelationId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_AuditEntries_EntityType_EntityId",
                table: "AuditLogs_AuditEntries",
                columns: new[] { "EntityType", "EntityId" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_AuditEntries_Module_Timestamp",
                table: "AuditLogs_AuditEntries",
                columns: new[] { "Module", "Timestamp" },
                descending: new[] { false, true }
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

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_AuditEntries_Timestamp",
                table: "AuditLogs_AuditEntries",
                column: "Timestamp",
                descending: new bool[0]
            );

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_AuditEntries_UserId_Timestamp",
                table: "AuditLogs_AuditEntries",
                columns: new[] { "UserId", "Timestamp" },
                descending: new[] { false, true }
            );

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobs_JobProgress_ModuleName",
                table: "BackgroundJobs_JobProgress",
                column: "ModuleName"
            );

            migrationBuilder.CreateIndex(
                name: "IX_JobQueueEntries_RecurringName",
                table: "BackgroundJobs_JobQueueEntries",
                column: "RecurringName"
            );

            migrationBuilder.CreateIndex(
                name: "IX_JobQueueEntries_State_ScheduledAt",
                table: "BackgroundJobs_JobQueueEntries",
                columns: new[] { "State", "ScheduledAt" }
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
                name: "IX_Email_EmailMessages_CreatedAt",
                table: "Email_EmailMessages",
                column: "CreatedAt"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Email_EmailMessages_Status",
                table: "Email_EmailMessages",
                column: "Status"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Email_EmailTemplates_Slug",
                table: "Email_EmailTemplates",
                column: "Slug",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlags_FeatureFlagOverrides_FlagName_OverrideType_OverrideValue",
                table: "FeatureFlags_FeatureFlagOverrides",
                columns: new[] { "FlagName", "OverrideType", "OverrideValue" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlags_FeatureFlags_Name",
                table: "FeatureFlags_FeatureFlags",
                column: "Name",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_FileStorage_StoredFiles_CreatedByUserId",
                table: "FileStorage_StoredFiles",
                column: "CreatedByUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_FileStorage_StoredFiles_Folder",
                table: "FileStorage_StoredFiles",
                column: "Folder"
            );

            migrationBuilder.CreateIndex(
                name: "IX_FileStorage_StoredFiles_Folder_FileName",
                table: "FileStorage_StoredFiles",
                columns: new[] { "Folder", "FileName" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Map_SavedMapBasemaps_SavedMapId",
                table: "Map_SavedMapBasemaps",
                column: "SavedMapId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Map_SavedMapLayers_SavedMapId",
                table: "Map_SavedMapLayers",
                column: "SavedMapId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_OpenIddict_OpenIddictApplications_ClientId",
                table: "OpenIddict_OpenIddictApplications",
                column: "ClientId",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_OpenIddict_OpenIddictAuthorizations_ApplicationId_Status_Subject_Type",
                table: "OpenIddict_OpenIddictAuthorizations",
                columns: new[] { "ApplicationId", "Status", "Subject", "Type" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_OpenIddict_OpenIddictScopes_Name",
                table: "OpenIddict_OpenIddictScopes",
                column: "Name",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_OpenIddict_OpenIddictTokens_ApplicationId_Status_Subject_Type",
                table: "OpenIddict_OpenIddictTokens",
                columns: new[] { "ApplicationId", "Status", "Subject", "Type" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_OpenIddict_OpenIddictTokens_AuthorizationId",
                table: "OpenIddict_OpenIddictTokens",
                column: "AuthorizationId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_OpenIddict_OpenIddictTokens_ReferenceId",
                table: "OpenIddict_OpenIddictTokens",
                column: "ReferenceId",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_PageBuilder_Pages_DeletedAt",
                table: "PageBuilder_Pages",
                column: "DeletedAt"
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

            migrationBuilder.CreateIndex(
                name: "IX_RateLimiting_Rules_PolicyName",
                table: "RateLimiting_Rules",
                column: "PolicyName",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Settings_PublicMenuItems_ParentId_SortOrder",
                table: "Settings_PublicMenuItems",
                columns: new[] { "ParentId", "SortOrder" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Settings_Settings_Key_Scope_UserId",
                table: "Settings_Settings",
                columns: new[] { "Key", "Scope", "UserId" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_TenantHosts_HostName",
                table: "Tenants_TenantHosts",
                column: "HostName",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_TenantHosts_TenantId",
                table: "Tenants_TenantHosts",
                column: "TenantId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Tenants_Slug",
                table: "Tenants_Tenants",
                column: "Slug",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Users_AspNetRoleClaims_RoleId",
                table: "Users_AspNetRoleClaims",
                column: "RoleId"
            );

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Users_AspNetRoles",
                column: "NormalizedName",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Users_AspNetUserClaims_UserId",
                table: "Users_AspNetUserClaims",
                column: "UserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Users_AspNetUserLogins_UserId",
                table: "Users_AspNetUserLogins",
                column: "UserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Users_AspNetUserPasskeys_UserId",
                table: "Users_AspNetUserPasskeys",
                column: "UserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Users_AspNetUserRoles_RoleId",
                table: "Users_AspNetUserRoles",
                column: "RoleId"
            );

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "Users_AspNetUsers",
                column: "NormalizedEmail"
            );

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Users_AspNetUsers",
                column: "NormalizedUserName",
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Agents_Messages");

            migrationBuilder.DropTable(name: "Agents_Sessions");

            migrationBuilder.DropTable(name: "AuditLogs_AuditEntries");

            migrationBuilder.DropTable(name: "BackgroundJobs_JobProgress");

            migrationBuilder.DropTable(name: "BackgroundJobs_JobQueueEntries");

            migrationBuilder.DropTable(name: "Chat_ChatMessages");

            migrationBuilder.DropTable(name: "Datasets_Datasets");

            migrationBuilder.DropTable(name: "Email_EmailMessages");

            migrationBuilder.DropTable(name: "Email_EmailTemplates");

            migrationBuilder.DropTable(name: "FeatureFlags_FeatureFlagOverrides");

            migrationBuilder.DropTable(name: "FeatureFlags_FeatureFlags");

            migrationBuilder.DropTable(name: "FileStorage_StoredFiles");

            migrationBuilder.DropTable(name: "Map_Basemaps");

            migrationBuilder.DropTable(name: "Map_LayerSources");

            migrationBuilder.DropTable(name: "Map_SavedMapBasemaps");

            migrationBuilder.DropTable(name: "Map_SavedMapLayers");

            migrationBuilder.DropTable(name: "OpenIddict_OpenIddictScopes");

            migrationBuilder.DropTable(name: "OpenIddict_OpenIddictTokens");

            migrationBuilder.DropTable(name: "Orders_OrderItems");

            migrationBuilder.DropTable(name: "PageBuilder_Tags");

            migrationBuilder.DropTable(name: "PageBuilder_Templates");

            migrationBuilder.DropTable(name: "Permissions_RolePermissions");

            migrationBuilder.DropTable(name: "Permissions_UserPermissions");

            migrationBuilder.DropTable(name: "Products_Products");

            migrationBuilder.DropTable(name: "Rag_CachedStructuredKnowledge");

            migrationBuilder.DropTable(name: "RateLimiting_Rules");

            migrationBuilder.DropTable(name: "Settings_PublicMenuItems");

            migrationBuilder.DropTable(name: "Settings_Settings");

            migrationBuilder.DropTable(name: "Tenants_TenantHosts");

            migrationBuilder.DropTable(name: "Users_AspNetRoleClaims");

            migrationBuilder.DropTable(name: "Users_AspNetUserClaims");

            migrationBuilder.DropTable(name: "Users_AspNetUserLogins");

            migrationBuilder.DropTable(name: "Users_AspNetUserPasskeys");

            migrationBuilder.DropTable(name: "Users_AspNetUserRoles");

            migrationBuilder.DropTable(name: "Users_AspNetUserTokens");

            migrationBuilder.DropTable(name: "Chat_Conversations");

            migrationBuilder.DropTable(name: "Map_SavedMaps");

            migrationBuilder.DropTable(name: "OpenIddict_OpenIddictAuthorizations");

            migrationBuilder.DropTable(name: "Orders_Orders");

            migrationBuilder.DropTable(name: "PageBuilder_Pages");

            migrationBuilder.DropTable(name: "Tenants_Tenants");

            migrationBuilder.DropTable(name: "Users_AspNetRoles");

            migrationBuilder.DropTable(name: "Users_AspNetUsers");

            migrationBuilder.DropTable(name: "OpenIddict_OpenIddictApplications");
        }
    }
}
