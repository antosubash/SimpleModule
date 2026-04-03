using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Endpoints;
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Endpoints.Messages;

public class GetAllMessagesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/messages",
                (IEmailContracts emailContracts) =>
                    CrudEndpoints.GetAll(emailContracts.GetAllMessagesAsync)
            )
            .RequirePermission(EmailPermissions.ViewHistory);
}
