using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using SimpleModule.Core;
using SimpleModule.Users.Constants;
using SimpleModule.Users.Entities;

namespace SimpleModule.Users.Endpoints.Users;

public class DownloadPersonalDataEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
                UsersConstants.RoutePrefix + UsersConstants.DownloadPersonalDataRoute,
                async (
                    ClaimsPrincipal principal,
                    UserManager<ApplicationUser> userManager,
                    ILogger<UsersModule> logger
                ) =>
                {
                    var user = await userManager.GetUserAsync(principal);
                    if (user is null)
                    {
                        return Results.NotFound();
                    }

                    logger.LogInformation("User asked for their personal data.");

                    // Manually enumerate personal data properties (AOT-compatible, no reflection)
                    var personalData = new Dictionary<string, string>
                    {
                        [PersonalDataKeys.Id] = user.Id ?? PersonalDataKeys.NullPlaceholder,
                        [PersonalDataKeys.UserName] =
                            user.UserName ?? PersonalDataKeys.NullPlaceholder,
                        [PersonalDataKeys.Email] = user.Email ?? PersonalDataKeys.NullPlaceholder,
                        [PersonalDataKeys.PhoneNumber] =
                            user.PhoneNumber ?? PersonalDataKeys.NullPlaceholder,
                        [PersonalDataKeys.DisplayName] = user.DisplayName,
                        [PersonalDataKeys.CreatedAt] = user.CreatedAt.ToString("O"),
                        [PersonalDataKeys.LastLoginAt] =
                            user.LastLoginAt?.ToString("O") ?? PersonalDataKeys.NullPlaceholder,
                    };

                    var logins = await userManager.GetLoginsAsync(user);
                    foreach (var l in logins)
                    {
                        personalData.Add(
                            $"{l.LoginProvider} {PersonalDataKeys.ExternalLoginSuffix}",
                            l.ProviderKey
                        );
                    }

                    personalData.Add(
                        PersonalDataKeys.AuthenticatorKey,
                        await userManager.GetAuthenticatorKeyAsync(user) ?? ""
                    );

                    return Results.File(
                        JsonSerializer.SerializeToUtf8Bytes(personalData),
                        PersonalDataKeys.PersonalDataContentType,
                        PersonalDataKeys.PersonalDataFileName
                    );
                }
            )
            .WithTags(UsersConstants.ModuleName)
            .RequireAuthorization();
    }
}
