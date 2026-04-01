using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SimpleModule.Host.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOrderSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Orders_OrderItems",
                keyColumns: new[] { "OrderId", "ProductId" },
                keyValues: new object[] { 1, 4 }
            );

            migrationBuilder.DeleteData(
                table: "Orders_OrderItems",
                keyColumns: new[] { "OrderId", "ProductId" },
                keyValues: new object[] { 1, 5 }
            );

            migrationBuilder.DeleteData(
                table: "Orders_OrderItems",
                keyColumns: new[] { "OrderId", "ProductId" },
                keyValues: new object[] { 1, 6 }
            );

            migrationBuilder.DeleteData(
                table: "Orders_OrderItems",
                keyColumns: new[] { "OrderId", "ProductId" },
                keyValues: new object[] { 2, 1 }
            );

            migrationBuilder.DeleteData(
                table: "Orders_OrderItems",
                keyColumns: new[] { "OrderId", "ProductId" },
                keyValues: new object[] { 3, 4 }
            );

            migrationBuilder.DeleteData(
                table: "Orders_OrderItems",
                keyColumns: new[] { "OrderId", "ProductId" },
                keyValues: new object[] { 3, 5 }
            );

            migrationBuilder.DeleteData(
                table: "Orders_OrderItems",
                keyColumns: new[] { "OrderId", "ProductId" },
                keyValues: new object[] { 4, 3 }
            );

            migrationBuilder.DeleteData(
                table: "Orders_OrderItems",
                keyColumns: new[] { "OrderId", "ProductId" },
                keyValues: new object[] { 5, 6 }
            );

            migrationBuilder.DeleteData(
                table: "Orders_OrderItems",
                keyColumns: new[] { "OrderId", "ProductId" },
                keyValues: new object[] { 5, 9 }
            );

            migrationBuilder.DeleteData(table: "Orders_Orders", keyColumn: "Id", keyValue: 1);

            migrationBuilder.DeleteData(table: "Orders_Orders", keyColumn: "Id", keyValue: 2);

            migrationBuilder.DeleteData(table: "Orders_Orders", keyColumn: "Id", keyValue: 3);

            migrationBuilder.DeleteData(table: "Orders_Orders", keyColumn: "Id", keyValue: 4);

            migrationBuilder.DeleteData(table: "Orders_Orders", keyColumn: "Id", keyValue: 5);

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
                    table: "Products_Products",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "PageBuilder_Templates",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "PageBuilder_Tags",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "PageBuilder_Pages",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "Orders_Orders",
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
                    table: "AuditLogs_AuditEntries",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .OldAnnotation("Sqlite:Autoincrement", true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                    table: "Products_Products",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "PageBuilder_Templates",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "PageBuilder_Tags",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "PageBuilder_Pages",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder
                .AlterColumn<int>(
                    name: "Id",
                    table: "Orders_Orders",
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
                    table: "AuditLogs_AuditEntries",
                    type: "INTEGER",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "INTEGER"
                )
                .Annotation("Sqlite:Autoincrement", true);

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
        }
    }
}
