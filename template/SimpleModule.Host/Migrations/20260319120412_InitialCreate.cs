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
                name: "Admin_AuditLogEntries",
                columns: table => new
                {
                    Id = table
                        .Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    PerformedByUserId = table.Column<string>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Details = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admin_AuditLogEntries", x => x.Id);
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
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
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
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", nullable: false),
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
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
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
                table: "Orders_Orders",
                columns: new[] { "Id", "CreatedAt", "Total", "UserId" },
                values: new object[,]
                {
                    {
                        1,
                        new DateTime(2026, 1, 2, 14, 0, 48, 324, DateTimeKind.Utc).AddTicks(4205),
                        4205.85m,
                        "2",
                    },
                    {
                        2,
                        new DateTime(2026, 1, 9, 10, 43, 3, 902, DateTimeKind.Utc).AddTicks(7351),
                        2752.49m,
                        "8",
                    },
                    {
                        3,
                        new DateTime(2026, 1, 27, 18, 48, 8, 543, DateTimeKind.Utc).AddTicks(1493),
                        192.31m,
                        "6",
                    },
                    {
                        4,
                        new DateTime(2026, 1, 19, 0, 29, 15, 501, DateTimeKind.Utc).AddTicks(3898),
                        3146.31m,
                        "2",
                    },
                    {
                        5,
                        new DateTime(2026, 1, 5, 1, 0, 8, 927, DateTimeKind.Utc).AddTicks(1333),
                        4580.92m,
                        "10",
                    },
                }
            );

            migrationBuilder.InsertData(
                table: "Products_Products",
                columns: new[] { "Id", "Name", "Price" },
                values: new object[,]
                {
                    { 1, "Fantastic Rubber Shoes", 99168m },
                    { 2, "Fantastic Rubber Bacon", 44622m },
                    { 3, "Fantastic Concrete Bike", 66012m },
                    { 4, "Handcrafted Concrete Keyboard", 63367m },
                    { 5, "Intelligent Frozen Mouse", 67430m },
                    { 6, "Sleek Soft Hat", 85163m },
                    { 7, "Practical Fresh Bike", 41748m },
                    { 8, "Handmade Steel Ball", 97556m },
                    { 9, "Ergonomic Fresh Pants", 92809m },
                    { 10, "Licensed Steel Sausages", 59260m },
                }
            );

            migrationBuilder.InsertData(
                table: "Orders_OrderItems",
                columns: new[] { "OrderId", "ProductId", "Quantity" },
                values: new object[,]
                {
                    { 1, 4, 5 },
                    { 1, 5, 1 },
                    { 1, 6, 1 },
                    { 2, 1, 3 },
                    { 3, 4, 2 },
                    { 3, 5, 4 },
                    { 4, 3, 1 },
                    { 5, 6, 5 },
                    { 5, 9, 3 },
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Admin_AuditLogEntries_Timestamp",
                table: "Admin_AuditLogEntries",
                column: "Timestamp"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Admin_AuditLogEntries_UserId",
                table: "Admin_AuditLogEntries",
                column: "UserId"
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
            migrationBuilder.DropTable(name: "Admin_AuditLogEntries");

            migrationBuilder.DropTable(name: "OpenIddict_OpenIddictScopes");

            migrationBuilder.DropTable(name: "OpenIddict_OpenIddictTokens");

            migrationBuilder.DropTable(name: "Orders_OrderItems");

            migrationBuilder.DropTable(name: "Permissions_RolePermissions");

            migrationBuilder.DropTable(name: "Permissions_UserPermissions");

            migrationBuilder.DropTable(name: "Products_Products");

            migrationBuilder.DropTable(name: "Users_AspNetRoleClaims");

            migrationBuilder.DropTable(name: "Users_AspNetUserClaims");

            migrationBuilder.DropTable(name: "Users_AspNetUserLogins");

            migrationBuilder.DropTable(name: "Users_AspNetUserRoles");

            migrationBuilder.DropTable(name: "Users_AspNetUserTokens");

            migrationBuilder.DropTable(name: "OpenIddict_OpenIddictAuthorizations");

            migrationBuilder.DropTable(name: "Orders_Orders");

            migrationBuilder.DropTable(name: "Users_AspNetRoles");

            migrationBuilder.DropTable(name: "Users_AspNetUsers");

            migrationBuilder.DropTable(name: "OpenIddict_OpenIddictApplications");
        }
    }
}
