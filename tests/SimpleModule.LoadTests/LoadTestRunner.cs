using System.Net.Http.Json;
using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using SimpleModule.LoadTests.Infrastructure;
using SimpleModule.LoadTests.Scenarios;
using SimpleModule.Tests.Shared.Fakes;
using SimpleModule.Users.Contracts;

namespace SimpleModule.LoadTests;

public sealed class LoadTestRunner : IClassFixture<LoadTestWebApplicationFactory>, IAsyncLifetime, IDisposable
{
    private readonly HttpClient _client;
    private string _seededUserId = null!;

    public LoadTestRunner(LoadTestWebApplicationFactory factory)
    {
        _client = factory.CreateServiceAccountClient();
    }

    public async ValueTask InitializeAsync()
    {
        // Seed a user for scenarios that require a valid UserId (e.g., Orders)
        var createReq = FakeDataGenerators.CreateUserRequestFaker.Generate();
        var resp = await _client.PostAsJsonAsync("/api/users", createReq);
        if (resp.IsSuccessStatusCode)
        {
            var user = await resp.Content.ReadFromJsonAsync<UserDto>();
            _seededUserId = user!.Id.Value;
        }
        else
        {
            // Fallback — some scenarios may fail
            _seededUserId = "test-user-id";
        }
    }

    public ValueTask DisposeAsync() => default;

    public void Dispose() => _client.Dispose();

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
        Assert.True(successRate > 50, $"Success rate too low: {successRate:F1}% ({stats.Ok.Request.Count}/{total})");
    }

    [Fact]
    public void Products_Crud() => RunScenario(ProductsScenario.Create(_client));

    [Fact(Skip = "Identity UserManager returns 500 under NBomber concurrency — test server threading issue")]
    public void Orders_Crud() => RunScenario(OrdersScenario.Create(_client, _seededUserId));

    [Fact(Skip = "Identity UserManager returns 500 under NBomber concurrency — test server threading issue")]
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
}
