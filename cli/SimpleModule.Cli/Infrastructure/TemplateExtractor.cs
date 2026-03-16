using System.Xml;
using System.Xml.Linq;

namespace SimpleModule.Cli.Infrastructure;

public static class TemplateExtractor
{
    /// <summary>
    /// Reads a file and replaces module names (plural first, then singular, then lowercase).
    /// </summary>
    public static string ReadAndTransform(
        string filePath,
        string fromPlural,
        string fromSingular,
        string toPlural,
        string toSingular,
        IReadOnlyList<string>? stripLinesContaining = null
    )
    {
        var lines = File.ReadAllLines(filePath).ToList();

        if (stripLinesContaining is { Count: > 0 })
        {
            lines.RemoveAll(line =>
                stripLinesContaining.Any(p => line.Contains(p, StringComparison.Ordinal))
            );
        }

        // Remove consecutive blank lines left by stripping
        lines = CollapseBlankLines(lines);

        var content = string.Join(Environment.NewLine, lines);
        return ReplaceModuleNames(content, fromPlural, fromSingular, toPlural, toSingular);
    }

    /// <summary>
    /// Reads a csproj, strips specified ProjectReference/PackageReference patterns, and renames.
    /// </summary>
    public static string TransformCsproj(
        string filePath,
        string fromModule,
        string toModule,
        IReadOnlyList<string>? stripReferencesContaining = null
    )
    {
        var doc = XDocument.Load(filePath);
        var root = doc.Root!;

        if (stripReferencesContaining is { Count: > 0 })
        {
            RemoveMatchingElements(root, "ProjectReference", "Include", stripReferencesContaining);
            RemoveMatchingElements(root, "PackageReference", "Include", stripReferencesContaining);

            // Remove empty ItemGroups
            foreach (var g in root.Elements("ItemGroup").Where(ig => !ig.HasElements).ToList())
            {
                g.Remove();
            }
        }

        var settings = new XmlWriterSettings
        {
            OmitXmlDeclaration = true,
            Indent = true,
            IndentChars = "  ",
        };

        using var sw = new StringWriter();
        using (var writer = XmlWriter.Create(sw, settings))
        {
            doc.Save(writer);
        }

        var result = sw.ToString();
        return result.Replace(fromModule, toModule, StringComparison.Ordinal);
    }

    /// <summary>
    /// Replaces plural name, singular name, and lowercase plural in content.
    /// </summary>
    public static string ReplaceModuleNames(
        string content,
        string fromPlural,
        string fromSingular,
        string toPlural,
        string toSingular
    )
    {
        // Replace plural first (e.g., "Orders" → "Invoices")
        content = content.Replace(fromPlural, toPlural, StringComparison.Ordinal);
        // Then singular (e.g., "Order" → "Invoice")
        content = content.Replace(fromSingular, toSingular, StringComparison.Ordinal);
        // Handle lowercase for routes (e.g., "orders" → "invoices")
        content = content.Replace(
            fromPlural.ToLowerInvariant(),
            toPlural.ToLowerInvariant(),
            StringComparison.Ordinal
        );
        return content;
    }

    /// <summary>
    /// Removes brace-balanced blocks starting from lines matching a predicate.
    /// </summary>
    public static List<string> RemoveBraceBlocks(
        List<string> lines,
        Func<string, bool> blockStartPredicate
    )
    {
        var result = new List<string>();
        var skipping = false;
        var braceDepth = 0;
        var seenOpenBrace = false;

        foreach (var line in lines)
        {
            if (!skipping && blockStartPredicate(line))
            {
                skipping = true;
                braceDepth = 0;
                seenOpenBrace = false;
                var opens = CountChar(line, '{');
                braceDepth += opens - CountChar(line, '}');

                if (opens > 0)
                {
                    seenOpenBrace = true;
                }

                // Single-line balanced statement (braces opened and closed on same line)
                if (seenOpenBrace && braceDepth <= 0)
                {
                    skipping = false;
                }

                continue;
            }

            if (skipping)
            {
                var opens2 = CountChar(line, '{');
                braceDepth += opens2 - CountChar(line, '}');

                if (opens2 > 0)
                {
                    seenOpenBrace = true;
                }

                if (seenOpenBrace && braceDepth <= 0)
                {
                    skipping = false;
                }

                continue;
            }

            result.Add(line);
        }

        return result;
    }

    /// <summary>
    /// Collapses runs of multiple blank lines into a single blank line.
    /// </summary>
    public static List<string> CollapseBlankLines(List<string> lines)
    {
        var result = new List<string>();
        var prevBlank = false;

        foreach (var line in lines)
        {
            var isBlank = string.IsNullOrWhiteSpace(line);
            if (isBlank && prevBlank)
            {
                continue;
            }

            result.Add(line);
            prevBlank = isBlank;
        }

        return result;
    }

    private static void RemoveMatchingElements(
        XElement root,
        string elementName,
        string attributeName,
        IReadOnlyList<string> patterns
    )
    {
        var toRemove = root.Descendants(elementName)
            .Where(el =>
                patterns.Any(p =>
                    el.Attribute(attributeName)
                        ?.Value?.Contains(p, StringComparison.OrdinalIgnoreCase) == true
                )
            )
            .ToList();

        foreach (var el in toRemove)
        {
            el.Remove();
        }
    }

    private static int CountChar(string s, char c)
    {
        var count = 0;
        foreach (var ch in s)
        {
            if (ch == c)
            {
                count++;
            }
        }

        return count;
    }
}
