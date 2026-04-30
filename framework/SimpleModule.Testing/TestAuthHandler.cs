using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SimpleModule.Testing;

/// <summary>
/// Authentication handler for integration tests. Reads claims from the
/// <see cref="TestAuthDefaults.ClaimsHeader"/> header (semicolon-separated
/// <c>type=value</c> pairs) and produces an authenticated <see cref="ClaimsPrincipal"/>.
/// Returns <see cref="AuthenticateResult.NoResult"/> when the header is absent so
/// other authentication schemes still get a chance to run.
/// </summary>
public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(TestAuthDefaults.ClaimsHeader, out var claimsHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>();
        foreach (var part in claimsHeader.ToString().Split(';'))
        {
            var kvp = part.Split('=', 2);
            if (kvp.Length == 2)
            {
                claims.Add(new Claim(kvp[0], kvp[1]));
            }
        }

        var identity = new ClaimsIdentity(claims, TestAuthDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, TestAuthDefaults.AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
