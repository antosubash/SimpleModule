using System.Net.Http.Json;
using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using SimpleModule.LoadTests.Infrastructure;
using SimpleModule.LoadTests.Scenarios;
using SimpleModule.Products.Contracts;
using SimpleModule.Tests.Shared.Fakes;

namespace SimpleModule.LoadTests;

public sealed class LoadTestRunner
    : IClassFixture<LoadTestWebApplicationFactory>,
        IAsyncLifetime,
        IDisposable
{
    private readonly LoadTestWebApplicationFactory _factory;
    private HttpClient _client = null!;
    private string _seededUserId = null!;
    private int[] _seededProductIds = null!;

    public LoadTestRunner(LoadTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async ValueTask InitializeAsync()
    {
        // Acquire a real Bearer token via ROPC (password grant)
        _client = await _factory.CreateBearerClientAsync();
        _seededUserId = await _factory.GetSeededUserIdAsync();

        // Seed products for the Orders scenario
        var productIds = new List<int>();
        for (var i = 0; i < 5; i++)
        {
            var req = FakeDataGenerators.CreateProductRequestFaker.Generate();
            var resp = await _client.PostAsJsonAsync("/api/products", req);
            if (resp.IsSuccessStatusCode)
            {
                var product = await resp.Content.ReadFromJsonAsync<Product>();
                productIds.Add(product!.Id.Value);
            }
        }

        _seededProductIds = productIds.Count > 0 ? [.. productIds] : [1];
    }

    public ValueTask DisposeAsync() => default;

    public void Dispose() => _client?.Dispose();

    private static void RunScenario(ScenarioProps scenario)
    {
        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("reports")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .Run();

        Assert.True(result.AllOkCount > 0, "No successful requests");
        var stats = result.ScenarioStats[0];
        var total = stats.Ok.Request.Count + stats.Fail.Request.Count;
        var successRate = total > 0 ? (double)stats.Ok.Request.Count / total * 100 : 0;
        Assert.True(
            successRate > 50,
            $"Success rate too low: {successRate:F1}% ({stats.Ok.Request.Count}/{total})"
        );
    }

    private static void RunAllScenarios(params ScenarioProps[] scenarios)
    {
        var result = NBomberRunner
            .RegisterScenarios(scenarios)
            .WithReportFolder("reports")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .Run();

        Assert.True(result.AllOkCount > 0, "No successful requests across all scenarios");

        foreach (var stats in result.ScenarioStats)
        {
            var total = stats.Ok.Request.Count + stats.Fail.Request.Count;
            var successRate = total > 0 ? (double)stats.Ok.Request.Count / total * 100 : 0;
            Assert.True(
                successRate > 50,
                $"Scenario '{stats.ScenarioName}' success rate too low: {successRate:F1}% ({stats.Ok.Request.Count}/{total})"
            );
        }
    }

    [Fact]
    public void All_Scenarios() =>
        RunAllScenarios(
            ProductsScenario.Create(_client, LoadProfile.Combined),
            OrdersScenario.Create(_client, _seededUserId, _seededProductIds, LoadProfile.Combined),
            UsersScenario.Create(_client, LoadProfile.Combined),
            SettingsScenario.Create(_client, LoadProfile.Combined),
            AuditLogsScenario.Create(_client, LoadProfile.Combined),
            FileStorageScenario.Create(_client, LoadProfile.Combined),
            PageBuilderScenario.Create(_client, LoadProfile.Combined),
            AdminScenario.Create(_client, LoadProfile.Combined),
            MixedWorkloadScenario.Create(_client, LoadProfile.Combined),
            FeatureFlagsScenario.Create(_client, LoadProfile.Combined),
            MarketplaceScenario.Create(_client, LoadProfile.Combined)
        );

    [Fact]
    public void Products_Crud() => RunScenario(ProductsScenario.Create(_client));

    [Fact]
    public void Orders_Crud() =>
        RunScenario(OrdersScenario.Create(_client, _seededUserId, _seededProductIds));

    [Fact]
    public void Users_Crud() => RunScenario(UsersScenario.Create(_client));

    [Fact]
    public void Settings_Ops() => RunScenario(SettingsScenario.Create(_client));

    [Fact]
    public void AuditLogs_Read() => RunScenario(AuditLogsScenario.Create(_client));

    [Fact]
    public void Files_Ops() => RunScenario(FileStorageScenario.Create(_client));

    [Fact]
    public void PageBuilder_Crud() => RunScenario(PageBuilderScenario.Create(_client));

    [Fact]
    public void Admin_Ops() => RunScenario(AdminScenario.Create(_client));

    [Fact]
    public void Mixed_Realistic() => RunScenario(MixedWorkloadScenario.Create(_client));

    [Fact]
    public void FeatureFlags_Ops() => RunScenario(FeatureFlagsScenario.Create(_client));

    [Fact]
    public void Marketplace_Read() => RunScenario(MarketplaceScenario.Create(_client));
}
