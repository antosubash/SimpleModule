using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SimpleModule.Keycloak.Services;

/// <summary>
/// Typed <see cref="HttpClient"/> wrapper for the Keycloak Admin REST API.
/// Token acquisition is delegated to the singleton <see cref="KeycloakTokenCache"/>
/// so that the cached token survives across transient HttpClient resolutions.
/// </summary>
public sealed partial class KeycloakAdminClient(
    HttpClient httpClient,
    IOptions<KeycloakOptions> options,
    KeycloakTokenCache tokenCache,
    ILogger<KeycloakAdminClient> logger
)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Returns the active sessions for a Keycloak user.
    /// GET /admin/realms/{realm}/users/{userId}/sessions
    /// </summary>
    public async Task<IReadOnlyList<KeycloakSessionDto>> GetUserSessionsAsync(
        string userId,
        CancellationToken cancellationToken = default
    )
    {
        var token = await tokenCache.GetTokenAsync(cancellationToken);

        var url = new Uri($"{options.Value.AdminApiBaseUrl.TrimEnd('/')}/users/{userId}/sessions");
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            LogGetSessionsFailed(logger, response.StatusCode, userId);
            return [];
        }

        var sessions = await response.Content.ReadFromJsonAsync<List<KeycloakSessionDto>>(
            JsonOptions,
            cancellationToken
        );

        return sessions ?? [];
    }

    /// <summary>
    /// Deletes a specific session by its Keycloak session ID.
    /// DELETE /admin/realms/{realm}/sessions/{sessionId}
    /// </summary>
    public async Task<bool> DeleteSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default
    )
    {
        var token = await tokenCache.GetTokenAsync(cancellationToken);

        var url = new Uri($"{options.Value.AdminApiBaseUrl.TrimEnd('/')}/sessions/{sessionId}");
        using var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            LogDeleteSessionFailed(logger, response.StatusCode, sessionId);
        }

        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Logs out a user from all sessions.
    /// POST /admin/realms/{realm}/users/{userId}/logout
    /// </summary>
    public async Task<bool> LogoutUserAsync(
        string userId,
        CancellationToken cancellationToken = default
    )
    {
        var token = await tokenCache.GetTokenAsync(cancellationToken);

        var url = new Uri($"{options.Value.AdminApiBaseUrl.TrimEnd('/')}/users/{userId}/logout");
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            LogLogoutFailed(logger, response.StatusCode, userId);
        }

        return response.IsSuccessStatusCode;
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Keycloak Admin API returned {StatusCode} for GET user sessions (userId={UserId})"
    )]
    private static partial void LogGetSessionsFailed(
        ILogger logger,
        System.Net.HttpStatusCode statusCode,
        string userId
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Keycloak Admin API returned {StatusCode} for DELETE session (sessionId={SessionId})"
    )]
    private static partial void LogDeleteSessionFailed(
        ILogger logger,
        System.Net.HttpStatusCode statusCode,
        string sessionId
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Keycloak Admin API returned {StatusCode} for POST logout (userId={UserId})"
    )]
    private static partial void LogLogoutFailed(
        ILogger logger,
        System.Net.HttpStatusCode statusCode,
        string userId
    );
}

/// <summary>
/// Represents a session returned by the Keycloak Admin REST API.
/// </summary>
public sealed class KeycloakSessionDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("ipAddress")]
    public string? IpAddress { get; set; }

    [JsonPropertyName("start")]
    public long? Start { get; set; }

    [JsonPropertyName("lastAccess")]
    public long? LastAccess { get; set; }

    [JsonPropertyName("clients")]
    public Dictionary<string, string>? Clients { get; set; }
}
