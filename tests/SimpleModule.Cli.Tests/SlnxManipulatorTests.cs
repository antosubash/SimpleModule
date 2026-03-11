using FluentAssertions;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed class SlnxManipulatorTests : IDisposable
{
    private readonly string _tempDir;

    public SlnxManipulatorTests()
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

    private string CreateSlnxFile(string content)
    {
        var path = Path.Combine(_tempDir, "Test.slnx");
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void HasModuleEntry_ReturnsFalse_WhenModuleNotPresent()
    {
        var path = CreateSlnxFile("""
            <Solution>
                <Folder Name="/modules/" />
            </Solution>
            """);

        SlnxManipulator.HasModuleEntry(path, "Invoices").Should().BeFalse();
    }

    [Fact]
    public void HasModuleEntry_ReturnsTrue_WhenModulePresent()
    {
        var path = CreateSlnxFile("""
            <Solution>
                <Folder Name="/modules/Invoices/">
                    <Project Path="src/modules/Invoices/Invoices/Invoices.csproj" />
                </Folder>
            </Solution>
            """);

        SlnxManipulator.HasModuleEntry(path, "Invoices").Should().BeTrue();
    }

    [Fact]
    public void HasModuleEntry_IsCaseInsensitive()
    {
        var path = CreateSlnxFile("""
            <Solution>
                <Folder Name="/modules/invoices/">
                    <Project Path="src/modules/invoices/invoices/invoices.csproj" />
                </Folder>
            </Solution>
            """);

        SlnxManipulator.HasModuleEntry(path, "Invoices").Should().BeTrue();
    }

    [Fact]
    public void AddModuleEntries_AddsModuleFolderBeforeTests()
    {
        var path = CreateSlnxFile("""
            <Solution>
                <Folder Name="/modules/" />
                <Folder Name="/tests/">
                    <Project Path="tests/Core.Tests/Core.Tests.csproj" />
                </Folder>
            </Solution>
            """);

        SlnxManipulator.AddModuleEntries(path, "Invoices");

        var content = File.ReadAllText(path);
        content.Should().Contain("/modules/Invoices/");
        content.Should().Contain("Invoices.Contracts/Invoices.Contracts.csproj");
        content.Should().Contain("Invoices/Invoices/Invoices.csproj");
    }

    [Fact]
    public void AddModuleEntries_AddsTestProject()
    {
        var path = CreateSlnxFile("""
            <Solution>
                <Folder Name="/modules/" />
                <Folder Name="/tests/modules/">
                    <Project Path="tests/modules/Orders.Tests/Orders.Tests.csproj" />
                </Folder>
            </Solution>
            """);

        SlnxManipulator.AddModuleEntries(path, "Invoices");

        var content = File.ReadAllText(path);
        content.Should().Contain("Invoices.Tests/Invoices.Tests.csproj");
    }

    [Fact]
    public void AddModuleEntries_DoesNotDuplicate()
    {
        var path = CreateSlnxFile("""
            <Solution>
                <Folder Name="/modules/Invoices/">
                    <Project Path="src/modules/Invoices/Invoices.Contracts/Invoices.Contracts.csproj" />
                    <Project Path="src/modules/Invoices/Invoices/Invoices.csproj" />
                </Folder>
                <Folder Name="/tests/" />
            </Solution>
            """);

        var contentBefore = File.ReadAllText(path);
        SlnxManipulator.AddModuleEntries(path, "Invoices");
        var contentAfter = File.ReadAllText(path);

        contentAfter.Should().Be(contentBefore, "should not modify the file when module already exists");
    }

    [Fact]
    public void AddModuleEntries_PreservesExistingContent()
    {
        var path = CreateSlnxFile("""
            <Solution>
                <Folder Name="/modules/Orders/">
                    <Project Path="src/modules/Orders/Orders/Orders.csproj" />
                </Folder>
                <Folder Name="/tests/" />
            </Solution>
            """);

        SlnxManipulator.AddModuleEntries(path, "Invoices");

        var content = File.ReadAllText(path);
        content.Should().Contain("/modules/Orders/");
        content.Should().Contain("Orders.csproj");
    }
}
