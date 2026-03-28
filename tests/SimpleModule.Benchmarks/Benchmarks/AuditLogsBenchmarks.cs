using BenchmarkDotNet.Attributes;
using SimpleModule.AuditLogs;
using SimpleModule.Tests.Shared.Fixtures;

namespace SimpleModule.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public sealed class AuditLogsBenchmarks : IDisposable
{
    private SimpleModuleWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [GlobalSetup]
    public void Setup()
    {
        _factory = new SimpleModuleWebApplicationFactory();
        _client = _factory.CreateAuthenticatedClient([
            AuditLogsPermissions.View,
            AuditLogsPermissions.Export,
        ]);
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
    public async Task<HttpResponseMessage> GetAllAuditLogs() =>
        await _client.GetAsync("/api/audit-logs");

    [Benchmark]
    public async Task<HttpResponseMessage> GetAuditLogStats() =>
        await _client.GetAsync("/api/audit-logs/stats");

    [Benchmark]
    public async Task<HttpResponseMessage> ExportAuditLogs() =>
        await _client.GetAsync("/api/audit-logs/export");
}
