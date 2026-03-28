namespace SimpleModule.LoadTests.Infrastructure;

/// <summary>
/// Manages the service account HttpClient for load testing.
/// Provides a pre-authenticated client with all permissions.
/// </summary>
public sealed class ServiceAccount : IDisposable
{
    private readonly LoadTestWebApplicationFactory _factory;

    public HttpClient Client { get; }

    public ServiceAccount()
    {
        LoadTestWebApplicationFactory.EnsureContentRoot();
        _factory = new LoadTestWebApplicationFactory();
        Client = _factory.CreateServiceAccountClient();
    }

    /// <summary>
    /// Creates an HttpMessageHandler that NBomber can use to make requests
    /// through the full ASP.NET pipeline.
    /// </summary>
    public HttpMessageHandler CreateHandler()
    {
        return _factory.Server.CreateHandler();
    }

    /// <summary>
    /// Base address of the test server.
    /// </summary>
    public Uri BaseAddress => _factory.Server.BaseAddress;

    public void Dispose()
    {
        Client.Dispose();
        _factory.Dispose();
    }
}
