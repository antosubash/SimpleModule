using Microsoft.AspNetCore.Routing;

namespace SimpleModule.Core;

public interface IViewEndpoint
{
    void Map(IEndpointRouteBuilder app);
}
