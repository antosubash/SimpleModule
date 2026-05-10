using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Testing;
using SimpleModule.Core.Authorization;

namespace SimpleModule.Testing;

public static class WebApplicationFactoryAuthExtensions
{
    /// <summary>
    /// Creates an <see cref="HttpClient"/> that carries the supplied claims as a
    /// <see cref="TestAuthDefaults.ClaimsHeader"/> header, which the
    /// <see cref="TestAuthHandler"/> turns into an authenticated principal.
    /// A default <see cref="ClaimTypes.NameIdentifier"/> is added if missing.
    /// </summary>
    public static HttpClient CreateAuthenticatedClient<TEntryPoint>(
        this WebApplicationFactory<TEntryPoint> factory,
        params Claim[] claims
    )
        where TEntryPoint : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(claims);

        var client = factory.CreateClient();
        ApplyClaims(client, claims);
        return client;
    }

    /// <summary>
    /// Same as <see cref="CreateAuthenticatedClient{TEntryPoint}(WebApplicationFactory{TEntryPoint}, Claim[])"/>
    /// but lets the caller pass <see cref="WebApplicationFactoryClientOptions"/>, e.g. to disable
    /// auto-redirect when the test asserts on the redirect itself.
    /// </summary>
    public static HttpClient CreateAuthenticatedClient<TEntryPoint>(
        this WebApplicationFactory<TEntryPoint> factory,
        WebApplicationFactoryClientOptions clientOptions,
        params Claim[] claims
    )
        where TEntryPoint : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(clientOptions);
        ArgumentNullException.ThrowIfNull(claims);

        var client = factory.CreateClient(clientOptions);
        ApplyClaims(client, claims);
        return client;
    }

    /// <summary>
    /// Convenience overload that adds each entry of <paramref name="permissions"/>
    /// as a <see cref="WellKnownClaims.Permission"/> claim before applying any
    /// <paramref name="additionalClaims"/>.
    /// </summary>
    public static HttpClient CreateAuthenticatedClient<TEntryPoint>(
        this WebApplicationFactory<TEntryPoint> factory,
        string[] permissions,
        params Claim[] additionalClaims
    )
        where TEntryPoint : class
    {
        ArgumentNullException.ThrowIfNull(permissions);
        ArgumentNullException.ThrowIfNull(additionalClaims);

        var combined = new List<Claim>(additionalClaims.Length + permissions.Length);
        combined.AddRange(additionalClaims);
        foreach (var permission in permissions)
        {
            combined.Add(new Claim(WellKnownClaims.Permission, permission));
        }
        return factory.CreateAuthenticatedClient(combined.ToArray());
    }

    private static void ApplyClaims(HttpClient client, IEnumerable<Claim> claims)
    {
        var list = new List<Claim>(claims);
        if (!list.Exists(c => c.Type == ClaimTypes.NameIdentifier))
        {
            list.Add(new Claim(ClaimTypes.NameIdentifier, "test-user-id"));
        }

        var headerValue = string.Join(";", list.Select(c => $"{c.Type}={c.Value}"));
        client.DefaultRequestHeaders.Remove(TestAuthDefaults.ClaimsHeader);
        client.DefaultRequestHeaders.Add(TestAuthDefaults.ClaimsHeader, headerValue);
    }
}
