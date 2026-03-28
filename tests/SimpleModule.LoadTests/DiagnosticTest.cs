using System.Net;
using System.Net.Http.Json;
using SimpleModule.LoadTests.Infrastructure;
using SimpleModule.Products.Contracts;
using SimpleModule.Tests.Shared.Fakes;

namespace SimpleModule.LoadTests;

public sealed class DiagnosticTest : IClassFixture<LoadTestWebApplicationFactory>, IDisposable
{
    private readonly HttpClient _client;

    public DiagnosticTest(LoadTestWebApplicationFactory factory)
    {
        _client = factory.CreateServiceAccountClient();
    }

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task Sequential_Crud_Works()
    {
        var req = FakeDataGenerators.CreateProductRequestFaker.Generate();
        var createResp = await _client.PostAsJsonAsync("/api/products", req);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);

        var product = await createResp.Content.ReadFromJsonAsync<Product>();
        var getResp = await _client.GetAsync($"/api/products/{product!.Id}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);

        var updateReq = FakeDataGenerators.UpdateProductRequestFaker.Generate();
        var updateResp = await _client.PutAsJsonAsync($"/api/products/{product.Id}", updateReq);
        Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);

        var deleteResp = await _client.DeleteAsync($"/api/products/{product.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);
    }

    [Fact]
    public async Task AuditLogs_Accessible()
    {
        var resp = await _client.GetAsync("/api/audit-logs");
        var body = await resp.Content.ReadAsStringAsync();
        Assert.True(resp.IsSuccessStatusCode, $"AuditLogs: {resp.StatusCode} - {body[..Math.Min(body.Length, 300)]}");
    }

    [Fact]
    public async Task AuditLogs_Stats_Accessible()
    {
        var resp = await _client.GetAsync("/api/audit-logs/stats");
        var body = await resp.Content.ReadAsStringAsync();
        Assert.True(resp.IsSuccessStatusCode, $"AuditLogs Stats: {resp.StatusCode} - {body[..Math.Min(body.Length, 300)]}");
    }

    [Fact]
    public async Task Orders_Crud_Works()
    {
        var req = FakeDataGenerators.CreateOrderRequestFaker.Generate();
        req.UserId = "service-account";
        var createResp = await _client.PostAsJsonAsync("/api/orders", req);
        var body = await createResp.Content.ReadAsStringAsync();
        Assert.True(createResp.IsSuccessStatusCode, $"Orders create: {createResp.StatusCode} - {body[..Math.Min(body.Length, 300)]}");
    }

    [Fact]
    public async Task Admin_CreateRole_Works()
    {
        using var form = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("name", $"TestRole-{Guid.NewGuid():N}"[..20]),
            new KeyValuePair<string, string>("description", "Diag test"),
        ]);
        var resp = await _client.PostAsync("/admin/roles/", form);
        var body = await resp.Content.ReadAsStringAsync();
        Assert.True(resp.IsSuccessStatusCode, $"Admin create role: {resp.StatusCode} - {body[..Math.Min(body.Length, 300)]}");
    }

    [Fact]
    public async Task Admin_Concurrent_Creates()
    {
        var tasks = Enumerable.Range(0, 5).Select(async i =>
        {
            using var form = new FormUrlEncodedContent([
                new KeyValuePair<string, string>("name", $"ConcRole-{Guid.NewGuid():N}"[..20]),
                new KeyValuePair<string, string>("description", "Concurrent test"),
            ]);
            var resp = await _client.PostAsync("/admin/roles/", form);
            var body = await resp.Content.ReadAsStringAsync();
            return $"[{i}] {resp.StatusCode}: {body[..Math.Min(body.Length, 200)]}";
        });

        var results = await Task.WhenAll(tasks);
        foreach (var r in results)
        {
            Assert.DoesNotContain("500", r, StringComparison.Ordinal);
            Assert.DoesNotContain("InternalServerError", r, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Admin_RapidFire_50_Requests()
    {
        // Simulate NBomber-like rapid fire: 50 sequential requests as fast as possible
        var failures = new List<string>();
        for (var i = 0; i < 50; i++)
        {
            using var form = new FormUrlEncodedContent([
                new KeyValuePair<string, string>("name", $"Rapid-{Guid.NewGuid():N}"[..20]),
                new KeyValuePair<string, string>("description", "Rapid fire test"),
            ]);
            var resp = await _client.PostAsync("/admin/roles/", form);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                failures.Add($"[{i}] {resp.StatusCode}: {body[..Math.Min(body.Length, 200)]}");
                if (failures.Count >= 3)
                    break; // Capture first 3 failures
            }
        }

        Assert.True(failures.Count == 0, $"Failures:\n{string.Join("\n", failures)}");
    }
}
