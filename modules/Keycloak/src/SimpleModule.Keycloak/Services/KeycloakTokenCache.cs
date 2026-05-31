using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace SimpleModule.Keycloak.Services;

/// <summary>
/// Singleton service that manages the Keycloak Admin REST API access token.
/// Extracted from <see cref="KeycloakAdminClient"/> so that the token survives
/// across transient <c>HttpClient</c> resolutions.
/// </summary>
public sealed class KeycloakTokenCache(
    IHttpClientFactory httpClientFactory,
    IOptions<KeycloakOptions> options
) : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_accessToken is not null && DateTimeOffset.UtcNow < _tokenExpiry)
            return _accessToken;

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (_accessToken is not null && DateTimeOffset.UtcNow < _tokenExpiry)
                return _accessToken;

            var opts = options.Value;
            var tokenUrl = new Uri($"{opts.Authority.TrimEnd('/')}/protocol/openid-connect/token");

            using var client = httpClientFactory.CreateClient();
            using var content = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = opts.AdminClientId,
                    ["client_secret"] = opts.AdminClientSecret,
                }
            );

            using var response = await client.PostAsync(tokenUrl, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var token = await response.Content.ReadFromJsonAsync<TokenResponse>(
                cancellationToken: cancellationToken
            );
            _accessToken =
                token?.AccessToken
                ?? throw new InvalidOperationException(
                    "Keycloak token response missing access_token."
                );

            // Expire the cached token 30 seconds early to avoid clock-skew issues.
            var expiresIn = Math.Max(0, (token.ExpiresIn > 30 ? token.ExpiresIn - 30 : 0));
            _tokenExpiry = DateTimeOffset.UtcNow.AddSeconds(expiresIn);

            return _accessToken;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose() => _semaphore.Dispose();

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1812:Avoid uninstantiated internal classes",
        Justification = "Instantiated by JSON deserialization"
    )]
    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn
    );
}
