using FluentAssertions;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed class ProjectManipulatorTests : IDisposable
{
    private readonly string _tempDir;

    public ProjectManipulatorTests()
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
    public void HasProjectReference_ReturnsFalse_WhenNotPresent()
    {
        var path = CreateCsprojFile(
            """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <ProjectReference Include="..\Core\Core.csproj" />
              </ItemGroup>
            </Project>
            """
        );

        ProjectManipulator.HasProjectReference(path, "Invoices").Should().BeFalse();
    }

    [Fact]
    public void HasProjectReference_ReturnsTrue_WhenPresent()
    {
        var path = CreateCsprojFile(
            """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <ProjectReference Include="..\modules\Invoices\Invoices\Invoices.csproj" />
              </ItemGroup>
            </Project>
            """
        );

        ProjectManipulator.HasProjectReference(path, "Invoices").Should().BeTrue();
    }

    [Fact]
    public void HasProjectReference_IsCaseInsensitive()
    {
        var path = CreateCsprojFile(
            """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <ProjectReference Include="..\modules\invoices\invoices\invoices.csproj" />
              </ItemGroup>
            </Project>
            """
        );

        ProjectManipulator.HasProjectReference(path, "Invoices").Should().BeTrue();
    }

    [Fact]
    public void AddProjectReference_AddsAfterLastReference()
    {
        var path = CreateCsprojFile(
            """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <ProjectReference Include="..\Core\Core.csproj" />
                <ProjectReference Include="..\modules\Orders\Orders\Orders.csproj" />
              </ItemGroup>
            </Project>
            """
        );

        ProjectManipulator.AddProjectReference(
            path,
            @"..\modules\Invoices\Invoices\Invoices.csproj"
        );

        var content = File.ReadAllText(path);
        content.Should().Contain("Invoices.csproj");
    }

    [Fact]
    public void AddProjectReference_DoesNotDuplicate()
    {
        var path = CreateCsprojFile(
            """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <ProjectReference Include="..\modules\Invoices\Invoices\Invoices.csproj" />
              </ItemGroup>
            </Project>
            """
        );

        ProjectManipulator.AddProjectReference(
            path,
            @"..\modules\Invoices\Invoices\Invoices.csproj"
        );

        var content = File.ReadAllText(path);
        var count = content.Split("Invoices.csproj").Length - 1;
        count.Should().Be(1, "should not duplicate the reference");
    }

    [Fact]
    public void AddProjectReference_CreatesItemGroup_WhenNoneExists()
    {
        var path = CreateCsprojFile(
            """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """
        );

        ProjectManipulator.AddProjectReference(
            path,
            @"..\modules\Invoices\Invoices\Invoices.csproj"
        );

        var content = File.ReadAllText(path);
        content.Should().Contain("<ItemGroup>");
        content.Should().Contain("Invoices.csproj");
    }

    [Fact]
    public void AddProjectReference_PreservesExistingReferences()
    {
        var path = CreateCsprojFile(
            """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <ProjectReference Include="..\Core\Core.csproj" />
              </ItemGroup>
            </Project>
            """
        );

        ProjectManipulator.AddProjectReference(
            path,
            @"..\modules\Invoices\Invoices\Invoices.csproj"
        );

        var content = File.ReadAllText(path);
        content.Should().Contain("Core.csproj");
        content.Should().Contain("Invoices.csproj");
    }
}
