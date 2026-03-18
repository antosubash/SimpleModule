using System;

namespace SimpleModule.Generator;

internal static class TypeMappingHelpers
{
    internal static string GetModuleFieldName(string fullyQualifiedName)
    {
        var name = fullyQualifiedName.Replace("global::", "").Replace(".", "_");
        return $"s_{name}";
    }

    internal static string MapCSharpTypeToTypeScript(
        string typeFqn,
        System.Collections.Generic.Dictionary<string, string>? knownDtoTypes = null
    )
    {
        var type = typeFqn.Replace("global::", "");

        // Nullable<T> -> T | null
        if (
            type.StartsWith("System.Nullable<", StringComparison.Ordinal)
            && type.EndsWith(">", StringComparison.Ordinal)
        )
        {
            var inner = type.Substring(
                "System.Nullable<".Length,
                type.Length - "System.Nullable<".Length - 1
            );
            return MapCSharpTypeToTypeScript("global::" + inner, knownDtoTypes) + " | null";
        }

        // Collection types
        if (
            type.StartsWith("System.Collections.Generic.List<", StringComparison.Ordinal)
            || type.StartsWith("System.Collections.Generic.IList<", StringComparison.Ordinal)
            || type.StartsWith("System.Collections.Generic.IEnumerable<", StringComparison.Ordinal)
            || type.StartsWith(
                "System.Collections.Generic.IReadOnlyList<",
                StringComparison.Ordinal
            )
            || type.StartsWith("System.Collections.Generic.ICollection<", StringComparison.Ordinal)
        )
        {
            var start = type.IndexOf('<') + 1;
            var inner = type.Substring(start, type.Length - start - 1);
            return MapCSharpTypeToTypeScript("global::" + inner, knownDtoTypes) + "[]";
        }

        // Check if this is a known [Dto] type
        if (knownDtoTypes is not null && knownDtoTypes.TryGetValue(typeFqn, out var tsName))
            return tsName;

        // Also try with global:: prefix
        if (
            knownDtoTypes is not null
            && knownDtoTypes.TryGetValue("global::" + type, out var tsName2)
        )
            return tsName2;

        return type switch
        {
            "string" or "System.String" => "string",
            "int" or "System.Int32" => "number",
            "long" or "System.Int64" => "number",
            "short" or "System.Int16" => "number",
            "byte" or "System.Byte" => "number",
            "float" or "System.Single" => "number",
            "double" or "System.Double" => "number",
            "decimal" or "System.Decimal" => "number",
            "bool" or "System.Boolean" => "boolean",
            "System.DateTime"
            or "System.DateTimeOffset"
            or "System.DateOnly"
            or "System.TimeOnly" => "string",
            "System.Guid" => "string",
            // Vogen strongly-typed IDs (map to their underlying primitive types)
            "SimpleModule.Core.Ids.ProductId" or "SimpleModule.Core.Ids.OrderId" => "number",
            "SimpleModule.Core.Ids.UserId" => "string",
            _ => "any",
        };
    }

    internal static string GetModuleNameFromFqn(string fqn)
    {
        // "global::SimpleModule.Products.Contracts.Product" -> "Products"
        var name = fqn.Replace("global::", "");
        var parts = name.Split('.');
        // Convention: SimpleModule.{ModuleName}.Contracts.{TypeName}
        // Return the second segment (module name)
        return parts.Length >= 3 ? parts[1] : parts[0];
    }
}
