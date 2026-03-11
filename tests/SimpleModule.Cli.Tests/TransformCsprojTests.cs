using FluentAssertions;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed class TransformCsprojTests : IDisposable
{
    private readonly string _tempDir;

    public TransformCsprojTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"sm-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    private string CreateCsprojFile(string content)
    {
        var path = Path.Combine(_tempDir, "Test.csproj");
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void RenamesModuleName()
    {
        var path = CreateCsprojFile(
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <ProjectReference Include="..\Orders.Contracts\Orders.Contracts.csproj" />
              </ItemGroup>
            </Project>
            """
        );

        var result = TemplateExtractor.TransformCsproj(path, "Orders", "Invoices");

        result.Should().Contain("Invoices.Contracts");
        result.Should().NotContain("Orders");
    }

    [Fact]
    public void StripsProjectReferences()
    {
        var path = CreateCsprojFile(
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <ProjectReference Include="..\Core\Core.csproj" />
                <ProjectReference Include="..\Bogus\Bogus.csproj" />
              </ItemGroup>
            </Project>
            """
        );

        var result = TemplateExtractor.TransformCsproj(path, "Orders", "Invoices", ["Bogus"]);

        result.Should().Contain("Core.csproj");
        result.Should().NotContain("Bogus");
    }

    [Fact]
    public void StripsPackageReferences()
    {
        var path = CreateCsprojFile(
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="FluentAssertions" />
                <PackageReference Include="Bogus" />
              </ItemGroup>
            </Project>
            """
        );

        var result = TemplateExtractor.TransformCsproj(path, "Orders", "Invoices", ["Bogus"]);

        result.Should().Contain("FluentAssertions");
        result.Should().NotContain("Bogus");
    }

    [Fact]
    public void RemovesEmptyItemGroups()
    {
        var path = CreateCsprojFile(
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="KeepThis" />
              </ItemGroup>
              <ItemGroup>
                <PackageReference Include="RemoveThis" />
              </ItemGroup>
            </Project>
            """
        );

        var result = TemplateExtractor.TransformCsproj(path, "X", "Y", ["RemoveThis"]);

        result.Should().Contain("KeepThis");
        result.Should().NotContain("RemoveThis");
        // The empty ItemGroup should be removed
        var itemGroupCount = result.Split("<ItemGroup").Length - 1;
        itemGroupCount.Should().Be(1);
    }

    [Fact]
    public void NoStripping_JustRenames()
    {
        var path = CreateCsprojFile(
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <RootNamespace>Orders</RootNamespace>
              </PropertyGroup>
            </Project>
            """
        );

        var result = TemplateExtractor.TransformCsproj(path, "Orders", "Invoices");

        result.Should().Contain("Invoices");
        result.Should().NotContain("Orders");
    }

    [Fact]
    public void OmitsXmlDeclaration()
    {
        var path = CreateCsprojFile(
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """
        );

        var result = TemplateExtractor.TransformCsproj(path, "X", "Y");

        result.Should().NotContain("<?xml");
    }
}
