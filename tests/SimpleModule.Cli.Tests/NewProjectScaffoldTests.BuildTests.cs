using System.Diagnostics;
using FluentAssertions;
using SimpleModule.Cli.Commands.New;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed partial class NewProjectScaffoldTests
{
    [Fact]
    public void ScaffoldedProject_DotnetBuildSucceeds()
    {
        var solution = SolutionContext.Discover();
        var frameworkVersion = NuGetVersionResolver.ResolveVersion(solution: solution);
        const string projectName = "TestApp";

        // When running in the repo, scaffold under the repo root so that
        // ProjectReferences to framework projects resolve correctly.
        // (macOS /var/folders temp dir can't reach /Volumes/... via relative paths)
        string rootDir;
        string? localTempDir = null;
        if (solution is not null)
        {
            localTempDir = Path.Combine(
                solution.RootPath,
                ".scaffold-test",
                Guid.NewGuid().ToString("N")
            );
            rootDir = Path.Combine(localTempDir, projectName);
        }
        else
        {
            rootDir = Path.Combine(_tempDir, projectName);
        }

        try
        {
            ScaffoldAndBuild(solution, frameworkVersion, projectName, rootDir);
        }
        finally
        {
            if (localTempDir is not null)
            {
                try
                {
                    Directory.Delete(localTempDir, recursive: true);
                }
                catch (IOException)
                {
                    // Best effort cleanup
                }
            }
        }
    }

    private static void ScaffoldAndBuild(
        SolutionContext? solution,
        string frameworkVersion,
        string projectName,
        string rootDir
    )
    {
        NewProjectCommand.ScaffoldProject(
            projectName,
            rootDir,
            solution: solution,
            frameworkVersion: frameworkVersion
        );

        // When running in the repo, replace NuGet PackageReferences with
        // ProjectReferences to the local framework so the test always builds
        // against the current source generator (not the published NuGet version).
        if (solution is not null)
        {
            PatchToLocalFrameworkReferences(rootDir, projectName, solution.RootPath);
        }

        // Clean obj directories to avoid stale NuGet restore artifacts
        // after patching ProjectReferences
        foreach (
            var objDir in Directory.GetDirectories(rootDir, "obj", SearchOption.AllDirectories)
        )
        {
            Directory.Delete(objDir, recursive: true);
        }

        var slnxPath = Path.Combine(rootDir, $"{projectName}.slnx");
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{slnxPath}\" /maxcpucount:1",
            WorkingDirectory = rootDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi)!;
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit(TimeSpan.FromSeconds(120));

        var buildOutput = $"""
            === STDOUT ===
            {stdout}
            === STDERR ===
            {stderr}
            """;

        process.ExitCode.Should().Be(0, $"dotnet build should succeed.\n{buildOutput}");
    }

    /// <summary>
    /// Replaces NuGet PackageReferences for SimpleModule framework packages with
    /// ProjectReferences to the local framework source. This ensures the scaffold
    /// build test validates against the current (possibly unpublished) generator.
    /// </summary>
    private static void PatchToLocalFrameworkReferences(
        string rootDir,
        string projectName,
        string repoRoot
    )
    {
        var frameworkDir = Path.Combine(repoRoot, "framework");

        PatchCsproj(
            Path.Combine(rootDir, "src", $"{projectName}.Host", $"{projectName}.Host.csproj"),
            frameworkDir,
            isHost: true
        );
        PatchCsproj(
            Path.Combine(rootDir, "src", "modules", "Items", "src", "Items", "Items.csproj"),
            frameworkDir,
            isHost: false
        );
        PatchCsproj(
            Path.Combine(
                rootDir,
                "src",
                "modules",
                "Items",
                "src",
                "Items.Contracts",
                "Items.Contracts.csproj"
            ),
            frameworkDir,
            isHost: false
        );

        // Remove framework PackageVersion entries from Directory.Packages.props
        // (they conflict with ProjectReferences)
        var packagesProps = Path.Combine(rootDir, "Directory.Packages.props");
        var propsLines = File.ReadAllLines(packagesProps).ToList();
        propsLines.RemoveAll(line =>
            line.Contains("SimpleModule.Core", StringComparison.Ordinal)
            || line.Contains("SimpleModule.Database", StringComparison.Ordinal)
            || line.Contains("SimpleModule.Hosting", StringComparison.Ordinal)
            || line.Contains("SimpleModule.Generator", StringComparison.Ordinal)
            || line.Contains("<!-- SimpleModule Framework -->", StringComparison.Ordinal)
        );
        File.WriteAllText(packagesProps, string.Join(Environment.NewLine, propsLines));
    }

    private static void PatchCsproj(string csprojPath, string frameworkDir, bool isHost)
    {
        var content = File.ReadAllText(csprojPath);
        var csprojDir = Path.GetDirectoryName(csprojPath)!;

        string RelRef(string project) =>
            Path.GetRelativePath(
                    csprojDir,
                    Path.Combine(frameworkDir, project, $"{project}.csproj")
                )
                .Replace('\\', '/');

        content = content
            .Replace(
                "<PackageReference Include=\"SimpleModule.Core\" />",
                $"<ProjectReference Include=\"{RelRef("SimpleModule.Core")}\" />",
                StringComparison.Ordinal
            )
            .Replace(
                "<PackageReference Include=\"SimpleModule.Database\" />",
                $"<ProjectReference Include=\"{RelRef("SimpleModule.Database")}\" />",
                StringComparison.Ordinal
            );

        if (isHost)
        {
            // The Host must reference framework assemblies that the generated code
            // may call into (Agents, Rag, Storage, DevTools, etc.).
            var hostRefs = new[]
            {
                "SimpleModule.Hosting",
                "SimpleModule.Agents",
                "SimpleModule.Rag",
                "SimpleModule.Rag.StructuredRag",
                "SimpleModule.Rag.VectorStore.InMemory",
                "SimpleModule.DevTools",
                "SimpleModule.Storage",
                "SimpleModule.Storage.Local",
            };
            var refsXml = string.Join(
                "\n    ",
                hostRefs.Select(r => $"<ProjectReference Include=\"{RelRef(r)}\" />")
            );

            content = content.Replace(
                "<PackageReference Include=\"SimpleModule.Hosting\" />",
                refsXml,
                StringComparison.Ordinal
            );
            content = content.Replace(
                "<PackageReference Include=\"SimpleModule.Generator\" OutputItemType=\"Analyzer\" ReferenceOutputAssembly=\"false\" PrivateAssets=\"all\" />",
                $"<ProjectReference Include=\"{RelRef("SimpleModule.Generator")}\" OutputItemType=\"Analyzer\" ReferenceOutputAssembly=\"false\" />",
                StringComparison.Ordinal
            );
        }

        File.WriteAllText(csprojPath, content);
    }
}
