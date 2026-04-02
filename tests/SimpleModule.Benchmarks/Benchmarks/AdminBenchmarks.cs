using System.Security.Claims;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Mvc.Testing;
using SimpleModule.Tests.Shared.Fixtures;

namespace SimpleModule.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public sealed class AdminBenchmarks : IDisposable
{
    private SimpleModuleWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private HttpClient _noRedirectClient = null!;

    [GlobalSetup]
    public void Setup()
    {
        _factory = new SimpleModuleWebApplicationFactory();
        _client = _factory.CreateAuthenticatedClient(new Claim(ClaimTypes.Role, "Admin"));

        _noRedirectClient = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
        );
        _noRedirectClient.DefaultRequestHeaders.Add(
            "X-Test-Claims",
            $"{ClaimTypes.NameIdentifier}=test-user-id;{ClaimTypes.Role}=Admin"
        );
    }

    [GlobalCleanup]
    public void Cleanup() => Dispose();

    public void Dispose()
    {
        _noRedirectClient?.Dispose();
        _client?.Dispose();
        _factory?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Benchmark]
    public async Task<HttpResponseMessage> CreateRole()
    {
        using var form = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("name", $"BenchRole-{Guid.NewGuid():N}"),
            new KeyValuePair<string, string>("description", "Benchmark test role"),
        ]);
        return await _client.PostAsync("/admin/roles/", form);
    }

    [Benchmark]
    public async Task CreateAndDeleteRole()
    {
        using var form = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("name", $"BenchRole-{Guid.NewGuid():N}"),
            new KeyValuePair<string, string>("description", "Benchmark test role"),
        ]);
        var response = await _noRedirectClient.PostAsync("/admin/roles/", form);
        var location = response.Headers.Location?.ToString();
        if (location is not null)
        {
            // Location is /admin/roles/{id}/edit — extract the id
            var segments = location.Split('/');
            var roleId = segments[^2]; // second-to-last segment
            await _noRedirectClient.DeleteAsync($"/admin/roles/{roleId}");
        }
    }

    [Benchmark]
    public async Task<HttpResponseMessage> CreateUser()
    {
        var email = $"bench-{Guid.NewGuid():N}@test.dev";
        using var form = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("email", email),
            new KeyValuePair<string, string>("displayName", "Benchmark User"),
            new KeyValuePair<string, string>("password", "BenchPass123!"),
            new KeyValuePair<string, string>("emailConfirmed", "true"),
        ]);
        return await _client.PostAsync("/admin/users/", form);
    }
}
