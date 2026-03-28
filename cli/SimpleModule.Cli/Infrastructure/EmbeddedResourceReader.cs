using System.Reflection;

namespace SimpleModule.Cli.Infrastructure;

/// <summary>
/// Reads template files embedded in the CLI assembly.
/// Resource names follow the pattern "Templates.Host.{path}" where path uses dots as separators.
/// </summary>
public static class EmbeddedResourceReader
{
    private static readonly Assembly CliAssembly = typeof(EmbeddedResourceReader).Assembly;

    /// <summary>
    /// Reads an embedded host template resource as a string.
    /// </summary>
    /// <param name="resourceName">Logical resource name (e.g., "Templates.Host.Program.cs")</param>
    public static string ReadTemplate(string resourceName)
    {
        using var stream = CliAssembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            throw new InvalidOperationException(
                $"Embedded resource '{resourceName}' not found in assembly."
            );
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Reads an embedded host template resource as lines.
    /// </summary>
    public static List<string> ReadTemplateLines(string resourceName)
    {
        var content = ReadTemplate(resourceName);
        return content.Split(["\r\n", "\n"], StringSplitOptions.None).ToList();
    }
}
