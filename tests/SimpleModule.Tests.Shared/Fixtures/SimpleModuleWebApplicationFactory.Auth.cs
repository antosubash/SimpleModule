using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SimpleModule.Tests.Shared.Fixtures;

public partial class SimpleModuleWebApplicationFactory
{
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
        EnsureDatabasesInitialized();
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

        return client;
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
        // Authenticate only test requests that include explicit test claims.
        if (!Request.Headers.ContainsKey("X-Test-Claims"))
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
