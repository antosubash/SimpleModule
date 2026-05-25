namespace SimpleModule.Keycloak.Contracts;

public static class KeycloakModuleConstants
{
    public const string ModuleName = "Keycloak";

    /// <summary>
    /// The authentication scheme name for the Keycloak OpenID Connect handler.
    /// </summary>
    public const string OidcSchemeName = "KeycloakOidc";

    public static class Routes
    {
        /// <summary>
        /// Initiates OIDC sign-in via Keycloak.
        /// </summary>
        public const string Login = "/keycloak/login";

        /// <summary>
        /// OIDC sign-in callback handled by the middleware.
        /// </summary>
        public const string Callback = "/keycloak/callback";

        /// <summary>
        /// Sign-out: clears local session and redirects to Keycloak end-session endpoint.
        /// </summary>
        public const string Logout = "/keycloak/logout";
    }
}
