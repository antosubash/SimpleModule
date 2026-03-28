using System.Net.Http.Json;
using System.Security.Claims;
using BenchmarkDotNet.Attributes;
using SimpleModule.Tests.Shared.Fakes;
using SimpleModule.Tests.Shared.Fixtures;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public sealed class UsersBenchmarks : IDisposable
{
    private SimpleModuleWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private string _seededUserId = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        _factory = new SimpleModuleWebApplicationFactory();
        _client = _factory.CreateAuthenticatedClient(
            new Claim(ClaimTypes.Role, "Admin")
        );

        var request = FakeDataGenerators.CreateUserRequestFaker.Generate();
        var response = await _client.PostAsJsonAsync("/api/users", request);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        _seededUserId = user!.Id.Value;
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
    public async Task<HttpResponseMessage> GetUserById() =>
        await _client.GetAsync($"/api/users/{_seededUserId}");

    [Benchmark]
    public async Task<HttpResponseMessage> UpdateUser()
    {
        var request = FakeDataGenerators.UpdateUserRequestFaker.Generate();
        return await _client.PutAsJsonAsync($"/api/users/{_seededUserId}", request);
    }

    [Benchmark]
    public async Task<HttpResponseMessage> CreateUser()
    {
        var request = FakeDataGenerators.CreateUserRequestFaker.Generate();
        return await _client.PostAsJsonAsync("/api/users", request);
    }
}
