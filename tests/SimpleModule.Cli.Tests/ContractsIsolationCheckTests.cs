using FluentAssertions;
using SimpleModule.Cli.Commands.Doctor.Checks;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed class ContractsIsolationCheckTests : IDisposable
{
    private readonly string _tempDir;

    public ContractsIsolationCheckTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"sm-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private SolutionContext CreateSolution(string moduleName, string contractsCsprojContent)
    {
        File.WriteAllText(Path.Combine(_tempDir, "Test.slnx"), "<Solution />");
        var modulesDir = Path.Combine(_tempDir, "src", "modules");
        var contractsDir = Path.Combine(modulesDir, moduleName, "src", $"{moduleName}.Contracts");
        Directory.CreateDirectory(contractsDir);
        File.WriteAllText(
            Path.Combine(contractsDir, $"{moduleName}.Contracts.csproj"),
            contractsCsprojContent
        );
        return SolutionContext.Discover(_tempDir)!;
    }

    [Fact]
    public void Run_Pass_WhenContractsOnlyRefsCore()
    {
        var solution = CreateSolution(
            "Products",
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <ProjectReference Include="..\..\..\..\src\SimpleModule.Core\SimpleModule.Core.csproj" />
              </ItemGroup>
            </Project>
            """
        );
        var results = new ContractsIsolationCheck().Run(solution).ToList();
        results
            .Should()
            .ContainSingle(r =>
                r.Name == "Products.Contracts isolation" && r.Status == CheckStatus.Pass
            );
    }

    [Fact]
    public void Run_Fail_WhenContractsRefsAnotherModuleProject()
    {
        var solution = CreateSolution(
            "Products",
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <ProjectReference Include="..\..\..\..\src\SimpleModule.Core\SimpleModule.Core.csproj" />
                <ProjectReference Include="..\..\..\Orders\src\Orders\Orders.csproj" />
              </ItemGroup>
            </Project>
            """
        );
        var results = new ContractsIsolationCheck().Run(solution).ToList();
        results
            .Should()
            .ContainSingle(r =>
                r.Name == "Products.Contracts isolation" && r.Status == CheckStatus.Fail
            );
    }

    [Fact]
    public void Run_Warning_WhenContractsCsprojNotFound()
    {
        File.WriteAllText(Path.Combine(_tempDir, "Test.slnx"), "<Solution />");
        var modulesDir = Path.Combine(_tempDir, "src", "modules");
        Directory.CreateDirectory(Path.Combine(modulesDir, "Products"));
        var solution = SolutionContext.Discover(_tempDir)!;
        var results = new ContractsIsolationCheck().Run(solution).ToList();
        results
            .Should()
            .ContainSingle(r =>
                r.Name == "Products.Contracts isolation" && r.Status == CheckStatus.Warning
            );
    }
}
