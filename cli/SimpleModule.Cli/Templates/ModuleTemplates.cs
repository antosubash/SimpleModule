using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Templates;

public sealed class ModuleTemplates
{
    private readonly SolutionContext? _solution;
    private readonly string? _refModule;
    private readonly string? _refSingular;
    private readonly IReadOnlyList<string> _otherModuleNames;

    public ModuleTemplates(SolutionContext? solution)
    {
        _solution = solution;
        _refModule =
            solution is not null && solution.ExistingModules.Count > 0
                ? solution.ExistingModules[0]
                : null;
        _refSingular = _refModule is not null ? GetSingularName(_refModule) : null;
        _otherModuleNames =
            _refModule is not null && solution is not null
                ? solution
                    .ExistingModules.Where(m =>
                        !string.Equals(m, _refModule, StringComparison.OrdinalIgnoreCase)
                    )
                    .ToList()
                : [];
    }

    // ── csproj files ─────────────────────────────────────────────────

    public string ContractsCsproj(string moduleName)
    {
        var refPath = RefContractsPath($"{_refModule}.Contracts.csproj");
        if (refPath is null)
        {
            return FallbackContractsCsproj();
        }

        return ReplaceFrameworkProjectRefs(
            TemplateExtractor.TransformCsproj(refPath, _refModule!, moduleName)
        );
    }

    public string ModuleCsproj(string moduleName)
    {
        var refPath = RefModulePath($"{_refModule}.csproj");
        if (refPath is null)
        {
            return FallbackModuleCsproj(moduleName);
        }

        // Strip references to other modules and non-essential packages
        var stripPatterns = _otherModuleNames.Select(m => m).Append("Bogus").ToList();

        return ReplaceFrameworkProjectRefs(
            TemplateExtractor.TransformCsproj(refPath, _refModule!, moduleName, stripPatterns)
        );
    }

    public string TestCsproj(string moduleName)
    {
        var refPath = RefTestPath($"{_refModule}.Tests.csproj");
        if (refPath is null)
        {
            return FallbackTestCsproj(moduleName);
        }

        var stripPatterns = _otherModuleNames.ToList();
        return ReplaceFrameworkProjectRefs(
            TemplateExtractor.TransformCsproj(refPath, _refModule!, moduleName, stripPatterns)
        );
    }

    // ── Simple C# files (read + rename) ──────────────────────────────

    public string DtoClass(string moduleName, string singularName)
    {
        var refPath = RefContractsPath($"{_refSingular}.cs");
        if (refPath is null)
        {
            return FallbackDtoClass(moduleName, singularName);
        }

        var lines = File.ReadAllLines(refPath).ToList();

        // Strip properties that reference complex types from the same module
        // (e.g., List<OrderItem> won't exist in the new module)
        var knownTypes = new HashSet<string>(StringComparer.Ordinal)
        {
            "int",
            "string",
            "decimal",
            "double",
            "float",
            "bool",
            "long",
            "DateTime",
            "DateTimeOffset",
            "Guid",
            "byte",
            "short",
        };

        lines.RemoveAll(line =>
        {
            var trimmed = line.TrimStart();
            if (
                !trimmed.StartsWith("public ", StringComparison.Ordinal)
                || !trimmed.Contains("{ get;", StringComparison.Ordinal)
            )
            {
                return false;
            }

            // Extract the type — it's the second word (after "public")
            var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
            {
                return false;
            }

            var typeName = parts[1];

            // Strip generic wrapper (List<T>, IList<T>, etc.)
            var genericStart = typeName.IndexOf('<', StringComparison.Ordinal);
            if (genericStart >= 0)
            {
                typeName = typeName[(genericStart + 1)..].TrimEnd('>');
            }

            // Strip nullable
            typeName = typeName.TrimEnd('?');

            return !knownTypes.Contains(typeName);
        });

        lines = TemplateExtractor.CollapseBlankLines(lines);

        var content = string.Join(Environment.NewLine, lines);
        return TemplateExtractor.ReplaceModuleNames(
            content,
            _refModule!,
            _refSingular!,
            moduleName,
            singularName
        );
    }

    public string EventClass(string moduleName, string singularName)
    {
        var refPath = RefContractsPath(Path.Combine("Events", $"{_refSingular}CreatedEvent.cs"));
        if (refPath is null)
        {
            return FallbackEventClass(moduleName, singularName);
        }

        return TemplateExtractor.ReadAndTransform(
            refPath,
            _refModule!,
            _refSingular!,
            moduleName,
            singularName
        );
    }

    public string GetAllEndpoint(string moduleName, string singularName)
    {
        var refPath = _refModule is not null
            ? RefModulePath(Path.Combine("Endpoints", _refModule, "GetAllEndpoint.cs"))
            : null;
        if (refPath is null)
        {
            return FallbackGetAllEndpoint(moduleName, singularName);
        }

        return TemplateExtractor.ReadAndTransform(
            refPath,
            _refModule!,
            _refSingular!,
            moduleName,
            singularName
        );
    }

    public string ConstantsClass(string moduleName, string singularName)
    {
        var refPath = RefModulePath($"{_refModule}Constants.cs");
        if (refPath is null)
        {
            return FallbackConstantsClass(moduleName, singularName);
        }

        return TemplateExtractor.ReadAndTransform(
            refPath,
            _refModule!,
            _refSingular!,
            moduleName,
            singularName
        );
    }

    public string GlobalUsings()
    {
        var refPath = RefTestPath("GlobalUsings.cs");
        if (refPath is not null)
        {
            return File.ReadAllText(refPath);
        }

        return """
            global using Xunit;
            """;
    }

    // ── Medium C# files (read + strip + rename) ─────────────────────

    public string ContractsInterface(string moduleName, string singularName)
    {
        var refPath = RefContractsPath($"I{_refSingular}Contracts.cs");
        if (refPath is null)
        {
            return FallbackContractsInterface(moduleName, singularName);
        }

        var lines = File.ReadAllLines(refPath).ToList();

        // Keep only the first method declaration — strip others
        var methodCount = 0;
        lines.RemoveAll(line =>
        {
            var trimmed = line.TrimStart();
            if (
                trimmed.StartsWith("Task<", StringComparison.Ordinal)
                || trimmed.StartsWith("Task ", StringComparison.Ordinal)
                || trimmed.StartsWith("ValueTask", StringComparison.Ordinal)
            )
            {
                methodCount++;
                return methodCount > 1;
            }

            return false;
        });

        var content = string.Join(Environment.NewLine, lines);
        return TemplateExtractor.ReplaceModuleNames(
            content,
            _refModule!,
            _refSingular!,
            moduleName,
            singularName
        );
    }

    public string ModuleClass(string moduleName, string singularName)
    {
        var refPath = RefModulePath($"{_refModule}Module.cs");
        if (refPath is null)
        {
            return FallbackModuleClass(moduleName, singularName);
        }

        var lines = File.ReadAllLines(refPath).ToList();

        // Strip using directives for Endpoints (auto-discovered, not needed in module)
        lines.RemoveAll(line =>
            line.Contains($".Endpoints.", StringComparison.Ordinal)
            && line.TrimStart().StartsWith("using ", StringComparison.Ordinal)
        );

        lines = TemplateExtractor.CollapseBlankLines(lines);

        var content = string.Join(Environment.NewLine, lines);
        return TemplateExtractor.ReplaceModuleNames(
            content,
            _refModule!,
            _refSingular!,
            moduleName,
            singularName
        );
    }

    // ── Complex C# files (read structure + strip + rename) ──────────

    public string DbContextClass(string moduleName, string singularName)
    {
        var refPath = RefModulePath($"{_refModule}DbContext.cs");
        if (refPath is null)
        {
            return FallbackDbContextClass(moduleName, singularName);
        }

        var lines = File.ReadAllLines(refPath).ToList();
        var otherModulePatterns = OtherModuleStripPatterns();
        otherModulePatterns.Add("using Bogus;");

        // Strip other module references
        lines.RemoveAll(line =>
            otherModulePatterns.Any(p => line.Contains(p, StringComparison.Ordinal))
        );

        // Keep only the first DbSet property
        var dbSetCount = 0;
        lines.RemoveAll(line =>
        {
            if (line.Contains("DbSet<", StringComparison.Ordinal))
            {
                dbSetCount++;
                return dbSetCount > 1;
            }

            return false;
        });

        // Find the primary entity name from the reference
        var primaryEntity = _refSingular!;

        // Remove entity config blocks for non-primary entities
        lines = TemplateExtractor.RemoveBraceBlocks(
            lines,
            line =>
                line.Contains("modelBuilder.Entity<", StringComparison.Ordinal)
                && !line.Contains($"modelBuilder.Entity<{primaryEntity}>", StringComparison.Ordinal)
        );

        // Remove Seed method calls
        lines.RemoveAll(line =>
            line.TrimStart().StartsWith("Seed", StringComparison.Ordinal)
            && line.Contains("(modelBuilder)", StringComparison.Ordinal)
        );

        // Remove Seed method definitions
        lines = TemplateExtractor.RemoveBraceBlocks(
            lines,
            line =>
                line.Contains("static void Seed", StringComparison.Ordinal)
                || line.Contains("static void seed", StringComparison.Ordinal)
        );

        // Simplify the primary entity config — keep only HasKey
        var inEntityConfig = false;
        var entityBraceDepth = 0;
        var keptHasKey = false;
        var simplifiedLines = new List<string>();

        foreach (var line in lines)
        {
            if (
                !inEntityConfig
                && line.Contains($"modelBuilder.Entity<{primaryEntity}>", StringComparison.Ordinal)
            )
            {
                inEntityConfig = true;
                entityBraceDepth = 0;
                keptHasKey = false;
                simplifiedLines.Add(line);
                entityBraceDepth += CountBraces(line);
                continue;
            }

            if (inEntityConfig)
            {
                entityBraceDepth += CountBraces(line);

                // Keep HasKey line and structural braces
                if (line.Contains("HasKey", StringComparison.Ordinal))
                {
                    keptHasKey = true;
                    simplifiedLines.Add(line);
                }
                else if (
                    line.TrimStart().StartsWith('{')
                    || line.TrimStart().StartsWith('}')
                    || line.TrimStart().StartsWith("});", StringComparison.Ordinal)
                )
                {
                    simplifiedLines.Add(line);
                }

                if (entityBraceDepth <= 0)
                {
                    inEntityConfig = false;
                    _ = keptHasKey; // Used above
                }

                continue;
            }

            simplifiedLines.Add(line);
        }

        simplifiedLines = TemplateExtractor.CollapseBlankLines(simplifiedLines);

        // Ensure brace balance — add closing braces if needed
        var braceBalance = 0;
        foreach (var line in simplifiedLines)
        {
            braceBalance += CountBraces(line);
        }

        while (braceBalance > 0)
        {
            simplifiedLines.Add("}");
            braceBalance--;
        }

        var content = string.Join(Environment.NewLine, simplifiedLines);
        return TemplateExtractor.ReplaceModuleNames(
            content,
            _refModule!,
            _refSingular!,
            moduleName,
            singularName
        );
    }

    public string ServiceClass(string moduleName, string singularName)
    {
        var refPath = RefModulePath($"{_refSingular}Service.cs");
        if (refPath is null)
        {
            return FallbackServiceClass(moduleName, singularName);
        }

        var lines = File.ReadAllLines(refPath).ToList();
        var otherModulePatterns = OtherModuleStripPatterns();
        otherModulePatterns.Add("using SimpleModule.Core.Events;");
        otherModulePatterns.Add("using SimpleModule.Core.Exceptions;");

        // Strip other module usings
        lines.RemoveAll(line =>
            line.TrimStart().StartsWith("using ", StringComparison.Ordinal)
            && otherModulePatterns.Any(p => line.Contains(p, StringComparison.Ordinal))
        );

        // Strip using for logging if present
        lines.RemoveAll(line =>
            line.Contains("using Microsoft.Extensions.Logging;", StringComparison.Ordinal)
        );

        // Find the class declaration and simplify constructor params
        // Keep only the DbContext param, remove cross-module and infrastructure deps
        var crossModuleTypes = _otherModuleNames
            .Select(m => $"I{GetSingularName(m)}")
            .Append("IEventBus")
            .Append("ILogger<")
            .ToList();

        lines.RemoveAll(line =>
            crossModuleTypes.Any(t => line.Contains(t, StringComparison.Ordinal))
        );

        // Simplify .Include() calls (navigation properties may not exist in new module)
        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].Contains(".Include(", StringComparison.Ordinal))
            {
                // Remove .Include(...) from the line
                var includeStart = lines[i].IndexOf(".Include(", StringComparison.Ordinal);
                var parenDepth = 0;
                var includeEnd = includeStart;

                for (var j = includeStart; j < lines[i].Length; j++)
                {
                    if (lines[i][j] == '(')
                    {
                        parenDepth++;
                    }
                    else if (lines[i][j] == ')')
                    {
                        parenDepth--;
                        if (parenDepth == 0)
                        {
                            includeEnd = j + 1;
                            break;
                        }
                    }
                }

                lines[i] = string.Concat(
                    lines[i].AsSpan(0, includeStart),
                    lines[i].AsSpan(includeEnd)
                );
            }
        }

        // Fix trailing comma on last constructor param
        FixTrailingComma(lines);

        // Remove 'partial' keyword from class declaration
        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].Contains("partial class", StringComparison.Ordinal))
            {
                lines[i] = lines[i].Replace("partial class", "class", StringComparison.Ordinal);
            }
        }

        // Remove LoggerMessage attributes and partial methods
        // These don't use braces ({/}), so remove them line-by-line
        var inAttribute = false;
        var inPartialMethod = false;
        var filteredLines = new List<string>();

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();

            if (trimmed.StartsWith("[LoggerMessage", StringComparison.Ordinal))
            {
                inAttribute = true;
            }

            if (inAttribute)
            {
                if (line.Contains(']', StringComparison.Ordinal))
                {
                    inAttribute = false;
                }

                continue;
            }

            if (trimmed.StartsWith("private static partial", StringComparison.Ordinal))
            {
                inPartialMethod = true;
            }

            if (inPartialMethod)
            {
                if (line.Contains(';', StringComparison.Ordinal))
                {
                    inPartialMethod = false;
                }

                continue;
            }

            filteredLines.Add(line);
        }

        lines = filteredLines;

        // Remove all methods except GetAll (expression-bodied or block-bodied)
        lines = TemplateExtractor.RemoveBraceBlocks(
            lines,
            line =>
            {
                var trimmed = line.TrimStart();
                if (
                    (
                        trimmed.StartsWith("public async", StringComparison.Ordinal)
                        || trimmed.StartsWith("public Task", StringComparison.Ordinal)
                        || trimmed.StartsWith("public ValueTask", StringComparison.Ordinal)
                    ) && !trimmed.Contains("GetAll", StringComparison.Ordinal)
                )
                {
                    return true;
                }

                return false;
            }
        );

        lines = TemplateExtractor.CollapseBlankLines(lines);

        // Ensure brace balance
        var braceBalance = 0;
        foreach (var line in lines)
        {
            braceBalance += CountBraces(line);
        }

        while (braceBalance > 0)
        {
            lines.Add("}");
            braceBalance--;
        }

        var content = string.Join(Environment.NewLine, lines);
        return TemplateExtractor.ReplaceModuleNames(
            content,
            _refModule!,
            _refSingular!,
            moduleName,
            singularName
        );
    }

    // ── Test files ──────────────────────────────────────────────────

    public string UnitTestSkeleton(string moduleName, string singularName)
    {
        var refPath = RefTestPath(Path.Combine("Unit", $"{_refSingular}ServiceTests.cs"));
        if (refPath is null)
        {
            return FallbackUnitTestSkeleton(moduleName, singularName);
        }

        // Read the reference for namespace convention
        var lines = File.ReadAllLines(refPath);
        var namespaceLine = lines.FirstOrDefault(l =>
            l.TrimStart().StartsWith("namespace ", StringComparison.Ordinal)
        );
        var refNamespace =
            namespaceLine?.Trim().TrimEnd(';') ?? $"namespace {_refModule}.Tests.Unit";

        var targetNamespace = refNamespace.Replace(
            _refModule!,
            moduleName,
            StringComparison.Ordinal
        );

        return $$"""
            using FluentAssertions;

            {{targetNamespace}};

            public sealed class {{singularName}}ServiceTests
            {
                [Fact]
                public void Placeholder_ShouldPass()
                {
                    true.Should().BeTrue();
                }
            }
            """;
    }

    public string IntegrationTestSkeleton(string moduleName, string singularName)
    {
        var refPath = RefTestPath(Path.Combine("Integration", $"{_refModule}EndpointTests.cs"));
        if (refPath is null)
        {
            return FallbackIntegrationTestSkeleton(moduleName, singularName);
        }

        var otherModulePatterns = OtherModuleStripPatterns();
        var lines = KeepFirstTestMethod(File.ReadAllLines(refPath).ToList(), otherModulePatterns);

        lines = TemplateExtractor.CollapseBlankLines(lines);

        var content = string.Join(Environment.NewLine, lines);
        return TemplateExtractor.ReplaceModuleNames(
            content,
            _refModule!,
            _refSingular!,
            moduleName,
            singularName
        );
    }

    // ── Utility ─────────────────────────────────────────────────────

    /// <summary>
    /// Derives a singular name from a plural module name.
    /// "Orders" → "Order", "Invoices" → "Invoice", "Users" → "User"
    /// </summary>
    public static string GetSingularName(string pluralName)
    {
        if (pluralName.EndsWith("ies", StringComparison.Ordinal))
        {
            return string.Concat(pluralName.AsSpan(0, pluralName.Length - 3), "y");
        }

        if (pluralName.EndsWith('s') && !pluralName.EndsWith("ss", StringComparison.Ordinal))
        {
            return pluralName[..^1];
        }

        return pluralName;
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private string? RefContractsPath(string relativePath)
    {
        if (_refModule is null || _solution is null)
        {
            return null;
        }

        var path = Path.Combine(_solution.GetModuleContractsPath(_refModule), relativePath);
        return File.Exists(path) ? path : null;
    }

    private string? RefModulePath(string relativePath)
    {
        if (_refModule is null || _solution is null)
        {
            return null;
        }

        var path = Path.Combine(_solution.GetModuleProjectPath(_refModule), relativePath);
        return File.Exists(path) ? path : null;
    }

    private string? RefTestPath(string relativePath)
    {
        if (_refModule is null || _solution is null)
        {
            return null;
        }

        var path = Path.Combine(_solution.GetTestProjectPath(_refModule), relativePath);
        return File.Exists(path) ? path : null;
    }

    /// <summary>
    /// Replaces framework ProjectReference entries (SimpleModule.Core, SimpleModule.Database, etc.)
    /// with PackageReference entries. Also adjusts Tests.Shared reference path.
    /// </summary>
    private static string ReplaceFrameworkProjectRefs(string csprojContent)
    {
        var lines = csprojContent.Split(["\r\n", "\n"], StringSplitOptions.None).ToList();

        // Map of framework project names to their NuGet package names
        var frameworkPackages = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["SimpleModule.Core"] = "SimpleModule.Core",
            ["SimpleModule.Database"] = "SimpleModule.Database",
            ["SimpleModule.Hosting"] = "SimpleModule.Hosting",
            ["SimpleModule.Blazor"] = "SimpleModule.Blazor",
            ["SimpleModule.DevTools"] = "SimpleModule.DevTools",
        };

        var result = new List<string>();
        foreach (var line in lines)
        {
            var replaced = false;
            if (line.Contains("<ProjectReference", StringComparison.Ordinal))
            {
                foreach (var (projectName, packageName) in frameworkPackages)
                {
                    if (line.Contains($"{projectName}.csproj", StringComparison.Ordinal))
                    {
                        // Detect indentation
                        var indent = line[..^line.TrimStart().Length];
                        result.Add($"{indent}<PackageReference Include=\"{packageName}\" />");
                        replaced = true;
                        break;
                    }
                }
            }

            // Strip Tests.Shared reference (not available as a NuGet package)
            if (
                !replaced
                && line.Contains("Tests.Shared", StringComparison.Ordinal)
                && line.Contains("<ProjectReference", StringComparison.Ordinal)
            )
            {
                replaced = true; // Skip this line entirely
            }

            if (!replaced)
            {
                result.Add(line);
            }
        }

        return string.Join(Environment.NewLine, TemplateExtractor.CollapseBlankLines(result));
    }

    private List<string> OtherModuleStripPatterns()
    {
        return _otherModuleNames.Select(m => $"SimpleModule.{m}").ToList();
    }

    private static void FixTrailingComma(List<string> lines)
    {
        // After stripping constructor params, the last remaining param line may have a trailing comma
        for (var i = 0; i < lines.Count - 1; i++)
        {
            var trimmedNext = lines[i + 1].TrimStart();
            if (trimmedNext.StartsWith(')') && lines[i].TrimEnd().EndsWith(','))
            {
                lines[i] = lines[i].TrimEnd()[..^1];
            }
        }
    }

    private static List<string> KeepFirstTestMethod(
        List<string> lines,
        List<string> otherModulePatterns
    )
    {
        // Strip cross-module usings
        lines.RemoveAll(line =>
            line.TrimStart().StartsWith("using ", StringComparison.Ordinal)
            && otherModulePatterns.Any(p => line.Contains(p, StringComparison.Ordinal))
        );

        // Find test methods and keep only the first one
        var result = new List<string>();
        var factCount = 0;
        var skipping = false;
        var braceDepth = 0;

        for (var i = 0; i < lines.Count; i++)
        {
            var trimmed = lines[i].TrimStart();

            if (trimmed.StartsWith("[Fact]", StringComparison.Ordinal))
            {
                factCount++;
                if (factCount > 1)
                {
                    // Skip this [Fact] and its method
                    skipping = true;
                    braceDepth = 0;
                    continue;
                }
            }

            if (skipping)
            {
                braceDepth += CountBraces(lines[i]);

                if (trimmed.StartsWith('}') && braceDepth <= 0)
                {
                    skipping = false;
                }

                continue;
            }

            result.Add(lines[i]);
        }

        return result;
    }

    private static int CountBraces(string line)
    {
        var count = 0;
        foreach (var c in line)
        {
            if (c == '{')
            {
                count++;
            }
            else if (c == '}')
            {
                count--;
            }
        }

        return count;
    }

    // ── Frontend config ─────────────────────────────────────────────

    public static string TsconfigJson() =>
        """
            {
              "extends": "@simplemodule/tsconfig/base",
              "compilerOptions": {
                "paths": {
                  "@/*": ["./*"]
                }
              }
            }
            """;

    // ── Fallback templates (when no reference module exists) ────────

    private static string FallbackContractsCsproj() =>
        """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <OutputType>Library</OutputType>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="SimpleModule.Core" />
              </ItemGroup>
            </Project>
            """;

    private static string FallbackModuleCsproj(string moduleName) =>
        $"""
            <Project Sdk="Microsoft.NET.Sdk.Razor">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <OutputType>Library</OutputType>
              </PropertyGroup>
              <ItemGroup>
                <FrameworkReference Include="Microsoft.AspNetCore.App" />
                <PackageReference Include="SimpleModule.Core" />
                <PackageReference Include="SimpleModule.Database" />
                <ProjectReference Include="..\{moduleName}.Contracts\{moduleName}.Contracts.csproj" />
              </ItemGroup>
            </Project>
            """;

    private static string FallbackTestCsproj(string moduleName) =>
        $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <IsPackable>false</IsPackable>
                <OutputType>Exe</OutputType>
              </PropertyGroup>
              <ItemGroup>
                <FrameworkReference Include="Microsoft.AspNetCore.App" />
              </ItemGroup>
              <ItemGroup>
                <PackageReference Include="xunit.v3" />
                <PackageReference Include="xunit.runner.visualstudio" />
                <PackageReference Include="Microsoft.NET.Test.Sdk" />
                <PackageReference Include="FluentAssertions" />
                <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" />
                <PackageReference Include="NSubstitute" />
              </ItemGroup>
              <ItemGroup>
                <ProjectReference Include="..\..\src\{moduleName}\{moduleName}.csproj" />
                <ProjectReference Include="..\..\src\{moduleName}.Contracts\{moduleName}.Contracts.csproj" />
              </ItemGroup>
            </Project>
            """;

    private static string FallbackContractsInterface(string moduleName, string singularName) =>
        $$"""
            namespace SimpleModule.{{moduleName}}.Contracts;

            public interface I{{singularName}}Contracts
            {
                Task<IEnumerable<{{singularName}}>> GetAll{{moduleName}}Async();
            }
            """;

    private static string FallbackDtoClass(string moduleName, string singularName) =>
        $$"""
            using SimpleModule.Core;

            namespace SimpleModule.{{moduleName}}.Contracts;

            [Dto]
            public class {{singularName}}
            {
                public int Id { get; set; }
                public string Name { get; set; } = string.Empty;
                public DateTime CreatedAt { get; set; }
            }
            """;

    private static string FallbackEventClass(string moduleName, string singularName) =>
        $$"""
            using SimpleModule.Core.Events;

            namespace SimpleModule.{{moduleName}}.Contracts.Events;

            public sealed record {{singularName}}CreatedEvent(int {{singularName}}Id) : IEvent;
            """;

    private static string FallbackModuleClass(string moduleName, string singularName) =>
        $$"""
            using Microsoft.Extensions.Configuration;
            using Microsoft.Extensions.DependencyInjection;
            using SimpleModule.Core;
            using SimpleModule.Database;
            using SimpleModule.{{moduleName}}.Contracts;

            namespace SimpleModule.{{moduleName}};

            [Module({{moduleName}}Constants.ModuleName, RoutePrefix = {{moduleName}}Constants.RoutePrefix)]
            public class {{moduleName}}Module : IModule
            {
                public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
                {
                    services.AddModuleDbContext<{{moduleName}}DbContext>(configuration, {{moduleName}}Constants.ModuleName);
                    services.AddScoped<I{{singularName}}Contracts, {{singularName}}Service>();
                }
            }
            """;

    private static string FallbackConstantsClass(string moduleName, string singularName) =>
        $$"""
            namespace SimpleModule.{{moduleName}};

            public static class {{moduleName}}Constants
            {
                public const string ModuleName = "{{moduleName}}";
                public const string RoutePrefix = "/api/{{moduleName.ToLowerInvariant()}}";
                public const string FieldName = "Name";
                public const string NameRequired = "Name is required.";
            }
            """;

    private static string FallbackDbContextClass(string moduleName, string singularName) =>
        $$"""
            using Microsoft.EntityFrameworkCore;
            using Microsoft.Extensions.Options;
            using SimpleModule.Database;
            using SimpleModule.{{moduleName}}.Contracts;

            namespace SimpleModule.{{moduleName}};

            public class {{moduleName}}DbContext(
                DbContextOptions<{{moduleName}}DbContext> options,
                IOptions<DatabaseOptions> dbOptions
            ) : DbContext(options)
            {
                public DbSet<{{singularName}}> {{moduleName}} => Set<{{singularName}}>();

                protected override void OnModelCreating(ModelBuilder modelBuilder)
                {
                    ArgumentNullException.ThrowIfNull(modelBuilder);

                    modelBuilder.Entity<{{singularName}}>(entity =>
                    {
                        entity.HasKey(e => e.Id);
                    });

                    modelBuilder.ApplyModuleSchema("{{moduleName}}", dbOptions.Value);
                }
            }
            """;

    private static string FallbackServiceClass(string moduleName, string singularName) =>
        $$"""
            using Microsoft.EntityFrameworkCore;
            using SimpleModule.{{moduleName}}.Contracts;

            namespace SimpleModule.{{moduleName}};

            public class {{singularName}}Service({{moduleName}}DbContext db) : I{{singularName}}Contracts
            {
                public async Task<IEnumerable<{{singularName}}>> GetAll{{moduleName}}Async() =>
                    await db.{{moduleName}}.ToListAsync().ConfigureAwait(false);
            }
            """;

    private static string FallbackGetAllEndpoint(string moduleName, string singularName) =>
        $$"""
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Http;
            using Microsoft.AspNetCore.Routing;
            using SimpleModule.Core;
            using SimpleModule.{{moduleName}}.Contracts;

            namespace SimpleModule.{{moduleName}}.Endpoints.{{moduleName}};

            public class GetAllEndpoint : IEndpoint
            {
                public void Map(IEndpointRouteBuilder app)
                {
                    app.MapGet(
                        "/",
                        async (I{{singularName}}Contracts contracts) =>
                        {
                            var items = await contracts.GetAll{{moduleName}}Async().ConfigureAwait(false);
                            return TypedResults.Ok(items);
                        }
                    ).AllowAnonymous();
                }
            }
            """;

    private static string FallbackUnitTestSkeleton(string moduleName, string singularName) =>
        $$"""
            using FluentAssertions;

            namespace {{moduleName}}.Tests.Unit;

            public sealed class {{singularName}}ServiceTests
            {
                [Fact]
                public void Placeholder_ShouldPass()
                {
                    true.Should().BeTrue();
                }
            }
            """;

    private static string FallbackIntegrationTestSkeleton(string moduleName, string singularName) =>
        $$"""
            using FluentAssertions;

            namespace {{moduleName}}.Tests.Integration;

            public sealed class {{moduleName}}EndpointTests
            {
                [Fact]
                public void Placeholder_ShouldPass()
                {
                    true.Should().BeTrue();
                }
            }
            """;
}
