using System;
using System.Reflection;
using System.Text.Json;

namespace SimpleModule.Core.Modules;

/// <summary>Thrown when a module manifest cannot be parsed or is incompatible.</summary>
public sealed class ModuleManifestException : Exception
{
    public ModuleManifestException(string message)
        : base(message) { }

    public ModuleManifestException(string message, Exception innerException)
        : base(message, innerException) { }

    public ModuleManifestException() { }
}

/// <summary>
/// Reads <see cref="ModuleManifest"/> instances from manifest JSON or from the
/// <see cref="ModuleManifestAttribute"/> on a module assembly.
/// </summary>
public static class ModuleManifestReader
{
    /// <summary>Highest manifest schema version this framework build understands.</summary>
    public const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static ModuleManifest Parse(string json)
    {
        ModuleManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<ModuleManifest>(json, SerializerOptions);
        }
        catch (JsonException ex)
        {
            throw new ModuleManifestException("Module manifest is not valid JSON.", ex);
        }

        if (manifest is null)
        {
            throw new ModuleManifestException("Module manifest JSON deserialized to null.");
        }

        if (manifest.SchemaVersion > CurrentSchemaVersion)
        {
            throw new ModuleManifestException(
                $"Module manifest schemaVersion {manifest.SchemaVersion} is newer than the "
                    + $"highest supported version {CurrentSchemaVersion}. Update the SimpleModule "
                    + "framework packages in the host to use this module."
            );
        }

        return manifest;
    }

    /// <summary>
    /// Reads the manifest from <paramref name="assembly"/>, or returns <c>null</c>
    /// when the assembly carries no <see cref="ModuleManifestAttribute"/> (e.g. a
    /// module compiled before manifest emission existed).
    /// </summary>
    public static ModuleManifest? TryRead(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        var attribute = assembly.GetCustomAttribute<ModuleManifestAttribute>();
        return attribute is null ? null : Parse(attribute.Json);
    }
}
