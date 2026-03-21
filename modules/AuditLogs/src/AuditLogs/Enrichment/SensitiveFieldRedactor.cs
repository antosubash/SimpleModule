using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace SimpleModule.AuditLogs.Enrichment;

public static partial class SensitiveFieldRedactor
{
    private const string Redacted = "[REDACTED]";

    [GeneratedRegex(
        @"password|secret|token|key|authorization|credential|ssn|credit.?card|cvv",
        RegexOptions.IgnoreCase
    )]
    private static partial Regex SensitivePattern();

    public static string? Redact(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            var node = JsonNode.Parse(json);
            if (node is null)
                return null;

            RedactNode(node);
            return node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void RedactNode(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            var keys = obj.Select(p => p.Key).ToList();
            foreach (var key in keys)
            {
                if (SensitivePattern().IsMatch(key))
                {
                    obj[key] = Redacted;
                }
                else if (obj[key] is JsonNode child)
                {
                    RedactNode(child);
                }
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                if (item is not null)
                {
                    RedactNode(item);
                }
            }
        }
    }
}
