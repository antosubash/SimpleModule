namespace SimpleModule.OpenIddict.Contracts;

public static class OpenIddictModuleConstants
{
    public const string ModuleName = "OpenIddict";
    public const string ViewPrefix = "/openiddict";

    public static class Routes
    {
        // View routes
        public const string Clients = "/clients";
        public const string ClientsCreate = "/clients/create";
        public const string ClientsEdit = "/clients/{id}/edit";

        // View route — registered via ConfigureEndpoints (escape hatch)
        public const string OAuthCallback = "/oauth-callback";

        // Connect routes (also in ConnectRouteConstants)
        public const string ConnectAuthorize = ConnectRouteConstants.ConnectAuthorize;
        public const string ConnectToken = ConnectRouteConstants.ConnectToken;
        public const string ConnectEndSession = ConnectRouteConstants.ConnectEndSession;
        public const string ConnectUserInfo = ConnectRouteConstants.ConnectUserInfo;
    }
}
