using System.Net.Http.Json;
using System.Security.Claims;
using BenchmarkDotNet.Attributes;
using SimpleModule.Core.Settings;
using SimpleModule.Tests.Shared.Fixtures;

namespace SimpleModule.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public sealed class SettingsBenchmarks : IDisposable
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
    public async Task<HttpResponseMessage> GetSettings() =>
        await _client.GetAsync("/api/settings");

    [Benchmark]
    public async Task<HttpResponseMessage> GetDefinitions() =>
        await _client.GetAsync("/api/settings/definitions");

    [Benchmark]
    public async Task<HttpResponseMessage> GetMenuTree() =>
        await _client.GetAsync("/api/settings/menus");

    [Benchmark]
    public async Task<HttpResponseMessage> GetAvailablePages() =>
        await _client.GetAsync("/api/settings/menus/available-pages");

    [Benchmark]
    public async Task<HttpResponseMessage> GetMySettings() =>
        await _client.GetAsync("/api/settings/me");

    [Benchmark]
    public async Task UpdateAndDeleteSetting()
    {
        var key = $"bench-{Guid.NewGuid():N}";
        var request = new { key, value = "bench-value", scope = SettingScope.Application };
        await _client.PutAsJsonAsync("/api/settings", request);
        await _client.DeleteAsync($"/api/settings/{key}?scope={SettingScope.Application}");
    }
}
