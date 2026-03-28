using System.Net.Http.Json;
using System.Security.Claims;
using BenchmarkDotNet.Attributes;
using SimpleModule.Tests.Shared.Fakes;
using SimpleModule.Tests.Shared.Fixtures;

namespace SimpleModule.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public sealed class UsersBenchmarks : IDisposable
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
    public async Task<HttpResponseMessage> GetAllUsers() =>
        await _client.GetAsync("/api/users");

    [Benchmark]
    public async Task<HttpResponseMessage> GetCurrentUser() =>
        await _client.GetAsync("/api/users/me");

    [Benchmark]
    public async Task<HttpResponseMessage> CreateUser()
    {
        var request = FakeDataGenerators.CreateUserRequestFaker.Generate();
        return await _client.PostAsJsonAsync("/api/users", request);
    }
}
