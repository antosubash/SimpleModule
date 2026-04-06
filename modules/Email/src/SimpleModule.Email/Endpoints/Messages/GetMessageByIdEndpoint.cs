using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Endpoints.Messages;

public class GetMessageByIdEndpoint : IEndpoint
{
    public const string Route = EmailConstants.Routes.GetMessageById;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                (int id, IEmailContracts emailContracts) =>
                    CrudEndpoints.GetById(() =>
                        emailContracts.GetMessageByIdAsync(EmailMessageId.From(id))
                    )
            )
            .RequirePermission(EmailPermissions.ViewHistory);
}
