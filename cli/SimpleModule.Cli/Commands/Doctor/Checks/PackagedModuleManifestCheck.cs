using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Commands.Doctor.Checks;

/// <summary>
/// Validates installed packaged modules: the manifest must be readable, at a
/// supported schemaVersion, and framework-compatible with this host.
/// </summary>
public sealed class PackagedModuleManifestCheck : IDoctorCheck
{
    private const int SupportedSchemaVersion = 1;

    public IEnumerable<CheckResult> Run(Infrastructure.SolutionContext solution)
    {
        var hostVersion = HostFrameworkVersionResolver.Resolve(solution.RootPath);

        foreach (
            var reference in PackageReferenceManipulator.GetPackageReferences(
                solution.ApiCsprojPath,
                solution.RootPath
            )
        )
        {
            // Framework/3rd-party packages have no manifest — only inspect ones
            // that look like SimpleModule modules to avoid scanning everything.
            var manifest = GlobalPackagesCache.TryReadManifest(reference.Id, reference.Version);
            if (manifest is null)
            {
                continue;
            }

            var name = $"Package {reference.Id}";
            if (manifest.SchemaVersion > SupportedSchemaVersion)
            {
                yield return new CheckResult(
                    name,
                    CheckStatus.Fail,
                    $"manifest schemaVersion {manifest.SchemaVersion} is newer than this tooling "
                        + $"supports ({SupportedSchemaVersion}) — update the SimpleModule framework/CLI"
                );
                continue;
            }

            if (hostVersion is not null)
            {
                var compat = FrameworkCompatChecker.Check(manifest.FrameworkCompat, hostVersion);
                if (!compat.Compatible)
                {
                    yield return new CheckResult(name, CheckStatus.Fail, compat.Reason);
                    continue;
                }
            }

            yield return new CheckResult(
                name,
                CheckStatus.Pass,
                $"{manifest.DisplayName} {reference.Version} (manifest v{manifest.SchemaVersion})"
            );
        }
    }
}
