using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SimpleModule.Host.Migrations
{
    /// <inheritdoc />
    public partial class AddMapChatDatasetsModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Visible = table.Column<bool>(type: "INTEGER", nullable: false),
                    Opacity = table.Column<double>(type: "REAL", nullable: false),
                    StyleOverrides = table.Column<string>(type: "TEXT", nullable: false),
                    SavedMapId = table.Column<Guid>(type: "TEXT", nullable: false),
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Chat_ChatMessages");

            migrationBuilder.DropTable(name: "Datasets_Datasets");

            migrationBuilder.DropTable(name: "Map_Basemaps");

            migrationBuilder.DropTable(name: "Map_LayerSources");

            migrationBuilder.DropTable(name: "Map_MapBasemap");

            migrationBuilder.DropTable(name: "Map_MapLayer");

            migrationBuilder.DropTable(name: "Chat_Conversations");

            migrationBuilder.DropTable(name: "Map_SavedMaps");
        }
    }
}
