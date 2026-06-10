using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace SimpleModule.Cli.Infrastructure;

/// <summary>
/// Lists EF Core migration ids ([Migration("...")] attribute values) declared
/// in an assembly, via System.Reflection.Metadata — no assembly loading.
/// </summary>
public static class AssemblyMigrationReader
{
    public static IReadOnlyList<string> ReadMigrationIds(string assemblyPath)
    {
        var ids = new List<string>();
        if (!File.Exists(assemblyPath))
        {
            return ids;
        }

        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        if (!peReader.HasMetadata)
        {
            return ids;
        }

        var reader = peReader.GetMetadataReader();
        foreach (var typeHandle in reader.TypeDefinitions)
        {
            var type = reader.GetTypeDefinition(typeHandle);
            foreach (var attrHandle in type.GetCustomAttributes())
            {
                var attribute = reader.GetCustomAttribute(attrHandle);
                if (!IsMigrationAttribute(reader, attribute))
                {
                    continue;
                }

                var blobReader = reader.GetBlobReader(attribute.Value);
                if (blobReader.ReadUInt16() == 0x0001)
                {
                    var id = blobReader.ReadSerializedString();
                    if (!string.IsNullOrEmpty(id))
                    {
                        ids.Add(id);
                    }
                }
            }
        }

        ids.Sort(StringComparer.Ordinal);
        return ids;
    }

    private static bool IsMigrationAttribute(MetadataReader reader, CustomAttribute attribute)
    {
        if (attribute.Constructor.Kind != HandleKind.MemberReference)
        {
            return false;
        }

        var member = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
        if (member.Parent.Kind != HandleKind.TypeReference)
        {
            return false;
        }

        var type = reader.GetTypeReference((TypeReferenceHandle)member.Parent);
        return reader.StringComparer.Equals(type.Name, "MigrationAttribute")
            && reader.StringComparer.Equals(
                type.Namespace,
                "Microsoft.EntityFrameworkCore.Migrations"
            );
    }
}
