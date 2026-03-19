using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleModule.Admin;
using SimpleModule.Host;
using SimpleModule.OpenIddict;
using SimpleModule.Orders;
using SimpleModule.PageBuilder;
using SimpleModule.Permissions;
using SimpleModule.Products;
using SimpleModule.Users;

namespace SimpleModule.Tests.Shared.Fixtures;

public class SimpleModuleWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestAuthScheme = "TestScheme";

    // Shared in-memory SQLite connection kept open for the lifetime of the factory
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            ReplaceDbContext<HostDbContext>(services, useOpenIddict: true);
            ReplaceDbContext<UsersDbContext>(services);
            ReplaceDbContext<OrdersDbContext>(services);
            ReplaceDbContext<ProductsDbContext>(services);
            ReplaceDbContext<AdminDbContext>(services);
            ReplaceDbContext<PageBuilderDbContext>(services);
            ReplaceDbContext<PermissionsDbContext>(services);
            ReplaceDbContext<OpenIddictAppDbContext>(services, useOpenIddict: true);

            // Remove hosted seed services — they need real DB tables that
            // EnsureCreated on HostDbContext alone won't produce for module contexts.
            RemoveHostedService<SimpleModule.OpenIddict.Services.OpenIddictSeedService>(services);
            RemoveHostedService<SimpleModule.Permissions.Services.PermissionSeedService>(services);

            // Add test authentication scheme that bypasses OpenIddict validation
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthScheme;
                    options.DefaultChallengeScheme = TestAuthScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthScheme, _ => { });
        });
    }

    public HttpClient CreateAuthenticatedClient(
        string[] permissions,
        params Claim[] additionalClaims
    )
    {
        var claims = new List<Claim>(additionalClaims);
        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }
        return CreateAuthenticatedClient(claims.ToArray());
    }

    public HttpClient CreateAuthenticatedClient(params Claim[] claims)
    {
        var client = CreateClient();
        var claimsList = new List<Claim>(claims);

        // Ensure there's always a Subject claim
        if (!claimsList.Exists(c => c.Type == ClaimTypes.NameIdentifier))
        {
            claimsList.Add(new Claim(ClaimTypes.NameIdentifier, "test-user-id"));
        }

        // Encode claims as a header the test handler will read
        var claimsValue = string.Join(";", claimsList.Select(c => $"{c.Type}={c.Value}"));
        client.DefaultRequestHeaders.Add("X-Test-Claims", claimsValue);
        client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");

        return client;
    }

    private static void RemoveHostedService<TService>(IServiceCollection services)
        where TService : class
    {
        var descriptor = services.SingleOrDefault(d =>
            d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)
            && d.ImplementationType == typeof(TService)
        );
        if (descriptor is not null)
        {
            services.Remove(descriptor);
        }
    }

    private void ReplaceDbContext<TContext>(IServiceCollection services, bool useOpenIddict = false)
        where TContext : DbContext
    {
        var descriptor = services.SingleOrDefault(d =>
            d.ServiceType == typeof(DbContextOptions<TContext>)
        );
        if (descriptor is not null)
        {
            services.Remove(descriptor);
        }

        services.AddDbContext<TContext>(options =>
        {
            options.UseSqlite(_connection);
            if (useOpenIddict)
            {
                options.UseOpenIddict();
            }
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }
}

public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Only authenticate if a Bearer token is present
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "test-user-id"),
            new(ClaimTypes.Name, "Test User"),
            new(ClaimTypes.Email, "test@example.com"),
        };

        // Parse custom claims from header
        if (Request.Headers.TryGetValue("X-Test-Claims", out var claimsHeader))
        {
            claims.Clear();
            var parts = claimsHeader.ToString().Split(';');
            foreach (var part in parts)
            {
                var kvp = part.Split('=', 2);
                if (kvp.Length == 2)
                {
                    claims.Add(new Claim(kvp[0], kvp[1]));
                }
            }
        }

        var identity = new ClaimsIdentity(claims, SimpleModuleWebApplicationFactory.TestAuthScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(
            principal,
            SimpleModuleWebApplicationFactory.TestAuthScheme
        );

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
