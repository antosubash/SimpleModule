using System;

namespace SimpleModule.Generator;

internal static class TypeMappingHelpers
{
    internal static string StripGlobalPrefix(string fqn) => fqn.Replace("global::", "");

    internal static string GetModuleFieldName(string fullyQualifiedName)
    {
        var name = StripGlobalPrefix(fullyQualifiedName).Replace(".", "_");
        return $"s_{name}";
    }

    internal static string MapCSharpTypeToTypeScript(
        string typeFqn,
        System.Collections.Generic.Dictionary<string, string>? knownDtoTypes = null
    )
    {
        var type = StripGlobalPrefix(typeFqn);

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
            // Strip any nested global:: prefix that Roslyn may add (e.g. int? -> Nullable<global::System.Int32>)
            inner = StripGlobalPrefix(inner);
            return MapCSharpTypeToTypeScript("global::" + inner, knownDtoTypes) + " | null";
        }

        // Dictionary types -> Record<K, V>
        if (
            type.StartsWith("System.Collections.Generic.Dictionary<", StringComparison.Ordinal)
            || type.StartsWith("System.Collections.Generic.IDictionary<", StringComparison.Ordinal)
            || type.StartsWith(
                "System.Collections.Generic.IReadOnlyDictionary<",
                StringComparison.Ordinal
            )
        )
        {
            var start = type.IndexOf('<') + 1;
            var inner = type.Substring(start, type.Length - start - 1);
            var commaIndex = FindTopLevelComma(inner);
            if (commaIndex >= 0)
            {
                var keyType = inner.Substring(0, commaIndex).Trim();
                var valueType = inner.Substring(commaIndex + 1).Trim();
                var tsKey = MapCSharpTypeToTypeScript("global::" + keyType, knownDtoTypes);
                var tsValue = MapCSharpTypeToTypeScript("global::" + valueType, knownDtoTypes);
                return $"Record<{tsKey}, {tsValue}>";
            }
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
            "object" or "System.Object" => "unknown",
            _ => "any",
        };
    }

    /// <summary>
    /// Finds the index of the first comma that is not nested inside angle brackets.
    /// Used to split generic type arguments like "string, int" in Dictionary&lt;string, int&gt;.
    /// </summary>
    private static int FindTopLevelComma(string text)
    {
        var depth = 0;
        for (var i = 0; i < text.Length; i++)
        {
            switch (text[i])
            {
                case '<':
                    depth++;
                    break;
                case '>':
                    depth--;
                    break;
                case ',' when depth == 0:
                    return i;
            }
        }

        return -1;
    }

    internal static string GetModuleNameFromFqn(string fqn)
    {
        // "global::SimpleModule.Products.Contracts.Product" -> "Products"
        var name = StripGlobalPrefix(fqn);
        var parts = name.Split('.');
        // Convention: SimpleModule.{ModuleName}.Contracts.{TypeName}
        // Return the second segment (module name)
        return parts.Length >= 3 ? parts[1] : parts[0];
    }
}
