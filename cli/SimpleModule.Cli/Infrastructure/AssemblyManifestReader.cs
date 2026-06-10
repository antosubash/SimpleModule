using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace SimpleModule.Cli.Infrastructure;

/// <summary>
/// Reads the <c>[assembly: ModuleManifest("{json}")]</c> attribute from a module
/// assembly using System.Reflection.Metadata — the assembly is never loaded, so
/// this works without resolving its dependencies (framework, ASP.NET, ...).
/// </summary>
public static class AssemblyManifestReader
{
    private const string AttributeNamespace = "SimpleModule.Core.Modules";
    private const string AttributeName = "ModuleManifestAttribute";

    public static ModuleManifestData? TryRead(string assemblyPath)
    {
        var json = TryReadJson(assemblyPath);
        return json is null ? null : ModuleManifestData.TryParse(json);
    }

    public static string? TryReadJson(string assemblyPath)
    {
        if (!File.Exists(assemblyPath))
        {
            return null;
        }

        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        if (!peReader.HasMetadata)
        {
            return null;
        }

        var reader = peReader.GetMetadataReader();
        foreach (var handle in reader.GetAssemblyDefinition().GetCustomAttributes())
        {
            var attribute = reader.GetCustomAttribute(handle);
            if (!IsModuleManifestAttribute(reader, attribute))
            {
                continue;
            }

            // Value blob: 0x0001 prolog, then the single string fixed argument.
            var blobReader = reader.GetBlobReader(attribute.Value);
            if (blobReader.ReadUInt16() != 0x0001)
            {
                return null;
            }

            return blobReader.ReadSerializedString();
        }

        return null;
    }

    private static bool IsModuleManifestAttribute(MetadataReader reader, CustomAttribute attribute)
    {
        StringHandle nameHandle;
        StringHandle namespaceHandle;

        switch (attribute.Constructor.Kind)
        {
            case HandleKind.MemberReference:
            {
                var member = reader.GetMemberReference(
                    (MemberReferenceHandle)attribute.Constructor
                );
                if (member.Parent.Kind != HandleKind.TypeReference)
                {
                    return false;
                }

                var type = reader.GetTypeReference((TypeReferenceHandle)member.Parent);
                nameHandle = type.Name;
                namespaceHandle = type.Namespace;
                break;
            }
            case HandleKind.MethodDefinition:
            {
                var method = reader.GetMethodDefinition(
                    (MethodDefinitionHandle)attribute.Constructor
                );
                var type = reader.GetTypeDefinition(method.GetDeclaringType());
                nameHandle = type.Name;
                namespaceHandle = type.Namespace;
                break;
            }
            default:
                return false;
        }

        return reader.StringComparer.Equals(nameHandle, AttributeName)
            && reader.StringComparer.Equals(namespaceHandle, AttributeNamespace);
    }
}
