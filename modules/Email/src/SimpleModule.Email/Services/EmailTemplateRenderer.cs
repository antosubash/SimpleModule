using System.Text.RegularExpressions;

namespace SimpleModule.Email.Services;

public static partial class EmailTemplateRenderer
{
    public static string Render(string template, Dictionary<string, string> variables)
    {
        return TemplateVariablePattern()
            .Replace(
                template,
                match =>
                {
                    var key = match.Groups[1].Value;
                    return variables.TryGetValue(key, out var value) ? value : match.Value;
                }
            );
    }

    [GeneratedRegex(@"\{\{(\w+)\}\}", RegexOptions.Compiled)]
    private static partial Regex TemplateVariablePattern();
}
