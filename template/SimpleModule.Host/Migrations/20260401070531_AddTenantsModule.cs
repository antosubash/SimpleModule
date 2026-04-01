using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SimpleModule.Host.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Tenants_TenantHosts");

            migrationBuilder.DropTable(name: "Tenants_Tenants");
        }
    }
}
