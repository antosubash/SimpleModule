using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Endpoints.Messages;

public class GetAllMessagesEndpoint : IEndpoint
{
    public const string Route = EmailConstants.Routes.GetAllMessages;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                (
                    [AsParameters] QueryEmailMessagesRequest request,
                    IEmailContracts emailContracts
                ) => emailContracts.QueryMessagesAsync(request)
            )
            .RequirePermission(EmailPermissions.ViewHistory);
}
