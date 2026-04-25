using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Items.Contracts;

namespace SimpleModule.Items.Endpoints.Items;

public class GetAllEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/",
                async (IItemContracts contracts) =>
                {
                    var items = await contracts.GetAllItemsAsync().ConfigureAwait(false);
                    return TypedResults.Ok(items);
                }
            )
            .AllowAnonymous();
    }
}
