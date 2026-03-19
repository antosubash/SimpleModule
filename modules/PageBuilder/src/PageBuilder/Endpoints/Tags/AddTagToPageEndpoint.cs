using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder.Endpoints.Tags;

public class AddTagToPageEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                "/{id}/tags",
                async (PageId id, AddTagRequest request, IPageBuilderContracts pageBuilder) =>
                {
                    if (string.IsNullOrWhiteSpace(request.Name))
                        throw new ArgumentException("Tag name is required.", nameof(request));

                    await pageBuilder.AddTagToPageAsync(id, request.Name);
                    return TypedResults.NoContent();
                }
            )
            .RequirePermission(PageBuilderPermissions.Update);
}
