using Microsoft.AspNetCore.Routing;

namespace SimpleModule.Core;

public interface IEndpoint
{
    void Map(IEndpointRouteBuilder app);
}
