# SimpleModule.Testing

Reusable integration-test helpers for SimpleModule applications.

## What's in the box

- `TestAuthHandler` — header-based test auth handler that turns a
  semicolon-separated list of `type=value` claims into an authenticated
  `ClaimsPrincipal`.
- `TestAuthDefaults` — scheme name (`TestScheme`) and header name
  (`X-Test-Claims`) constants.
- `services.AddTestAuthentication()` — registers the handler as the default
  authenticate/challenge scheme.
- `factory.CreateAuthenticatedClient(params Claim[])` and
  `factory.CreateAuthenticatedClient(string[] permissions, params Claim[])`
  extensions on `WebApplicationFactory<TEntryPoint>` that produce HTTP clients
  with the appropriate `X-Test-Claims` header.

## Usage

```csharp
using SimpleModule.Testing;

public sealed class MyAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddTestAuthentication();
        });
    }
}

public sealed class MyEndpointTests(MyAppFactory factory) : IClassFixture<MyAppFactory>
{
    [Fact]
    public async Task Authenticated_request_succeeds()
    {
        var client = factory.CreateAuthenticatedClient(
            new Claim("permission", "Things.View")
        );

        var response = await client.GetAsync("/api/things");

        response.EnsureSuccessStatusCode();
    }
}
```

This package is intentionally narrow: it ships only the auth-related plumbing.
Wire your own `WebApplicationFactory` for module-specific setup (DB swap,
hosted-service removal, etc.).

## License

MIT
