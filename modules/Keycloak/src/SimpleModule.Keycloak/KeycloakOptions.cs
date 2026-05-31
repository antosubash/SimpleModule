namespace SimpleModule.Keycloak;

/// <summary>
/// Configuration options for the Keycloak identity provider module.
/// Bound from the "Keycloak" section of <c>appsettings.json</c>.
/// </summary>
public sealed class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    /// <summary>
    /// The OpenID Connect authority URL, typically <c>https://keycloak.example.com/realms/{realm}</c>.
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// OAuth2 client ID registered in Keycloak for this application.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// OAuth2 client secret for the application client (confidential client).
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Keycloak realm name (e.g. "simplemodule").
    /// </summary>
    public string Realm { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for the Keycloak Admin REST API, e.g.
    /// <c>https://keycloak.example.com/admin/realms/{realm}</c>.
    /// Kept as <see cref="string"/> for configuration binding compatibility.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1056:URI-like properties should not be strings",
        Justification = "Configuration binding requires string"
    )]
    public string AdminApiBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Service-account client ID used for Admin REST API calls.
    /// </summary>
    public string AdminClientId { get; set; } = string.Empty;

    /// <summary>
    /// Service-account client secret used for Admin REST API calls.
    /// </summary>
    public string AdminClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Whether to require HTTPS for the OpenID Connect metadata endpoint.
    /// Defaults to <c>true</c>; set to <c>false</c> only for local development.
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;
}
