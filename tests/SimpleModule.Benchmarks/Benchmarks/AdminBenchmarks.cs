using System.Security.Claims;
using BenchmarkDotNet.Attributes;
using SimpleModule.Tests.Shared.Fixtures;

namespace SimpleModule.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public sealed class AdminBenchmarks : IDisposable
{
    private SimpleModuleWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [GlobalSetup]
    public void Setup()
    {
        _factory = new SimpleModuleWebApplicationFactory();
        _client = _factory.CreateAuthenticatedClient(
            new Claim(ClaimTypes.Role, "Admin")
        );
    }

    [GlobalCleanup]
    public void Cleanup() => Dispose();

    public void Dispose()
    {
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
