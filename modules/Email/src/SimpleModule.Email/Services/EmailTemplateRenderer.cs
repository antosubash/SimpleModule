using System.Net;
using System.Text.RegularExpressions;

namespace SimpleModule.Email.Services;

public static partial class EmailTemplateRenderer
{
    public static string Render(string template, Dictionary<string, string> variables, bool isHtml)
    {
        return TemplateVariablePattern()
            .Replace(
                template,
                match =>
                {
                    var key = match.Groups[1].Value;
                    if (!variables.TryGetValue(key, out var value))
                        return match.Value;
                    return isHtml ? WebUtility.HtmlEncode(value) : value;
                }
            );
    }

    public static HashSet<string> ExtractVariables(string template)
    {
        var matches = TemplateVariablePattern().Matches(template);
        return matches.Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);
    }

    [GeneratedRegex(@"\{\{(\w+)\}\}")]
    private static partial Regex TemplateVariablePattern();
}
