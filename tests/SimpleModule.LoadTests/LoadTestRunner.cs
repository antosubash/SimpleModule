using System.Net.Http.Json;
using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using SimpleModule.LoadTests.Infrastructure;
using SimpleModule.LoadTests.Scenarios;
using SimpleModule.Products.Contracts;
using SimpleModule.Tests.Shared.Fakes;
using SimpleModule.Users.Contracts;

namespace SimpleModule.LoadTests;

public sealed class LoadTestRunner : IClassFixture<LoadTestWebApplicationFactory>, IAsyncLifetime, IDisposable
{
    private readonly LoadTestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private HttpClient _identityClient = null!;
    private string _seededUserId = null!;
    private int[] _seededProductIds = null!;

    public LoadTestRunner(LoadTestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateServiceAccountClient();
    }

    public async ValueTask InitializeAsync()
    {
        // Seed a real Identity user for endpoints that look up the user via UserManager
        var createUserReq = FakeDataGenerators.CreateUserRequestFaker.Generate();
        var userResp = await _client.PostAsJsonAsync("/api/users", createUserReq);
        if (userResp.IsSuccessStatusCode)
        {
            var user = await userResp.Content.ReadFromJsonAsync<UserDto>();
            _seededUserId = user!.Id.Value;
        }
        else
        {
            _seededUserId = "test-user-id";
        }

        // Create a client whose NameIdentifier matches the seeded user
        _identityClient = _factory.CreateServiceAccountClientWithUserId(_seededUserId);

        // Seed products for the Orders scenario (OrderItems reference ProductIds)
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

    public void Dispose()
    {
        _client.Dispose();
        _identityClient?.Dispose();
    }

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

    [Fact]
    public void Orders_Crud() => RunScenario(OrdersScenario.Create(_identityClient, _seededUserId, _seededProductIds));

    [Fact]
    public void Users_Crud() => RunScenario(UsersScenario.Create(_identityClient));

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
