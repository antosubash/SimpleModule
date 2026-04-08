using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Pages;

public class SettingsEndpoint : IViewEndpoint
{
    public const string Route = EmailConstants.Routes.Settings;

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                Route,
                (IOptions<EmailModuleOptions> options) =>
                {
                    var opts = options.Value;
                    return Inertia.Render(
                        "Email/Settings",
                        new
                        {
                            provider = opts.Provider,
                            defaultFromAddress = opts.DefaultFromAddress,
                        }
                    );
                }
            )
            .RequirePermission(EmailPermissions.Send);
    }
}
