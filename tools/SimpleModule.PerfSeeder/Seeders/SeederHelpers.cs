using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace SimpleModule.PerfSeeder.Seeders;

internal static class SeederHelpers
{
    /// <summary>
    /// Returns the provider-appropriate quoted table identifier (e.g. <c>"products"."Products"</c>
    /// under PostgreSQL, <c>"Products_Products"</c> under SQLite).
    /// </summary>
    public static string QuoteTable(this DbContext ctx, Type entityType)
    {
        var et =
            ctx.Model.FindEntityType(entityType)
            ?? throw new InvalidOperationException(
                $"Entity type {entityType.Name} is not part of the model."
            );
        return QuoteTable(et);
    }

    public static string QuoteTable(IEntityType et)
    {
        var table =
            et.GetTableName()
            ?? throw new InvalidOperationException($"Entity {et.ClrType.Name} has no table name.");
        var schema = et.GetSchema();
        return schema is null ? $"\"{table}\"" : $"\"{schema}\".\"{table}\"";
    }
}
