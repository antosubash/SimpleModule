using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace SimpleModule.Core.FormRequests;

public static class FormRequestExtensions
{
    public static RouteGroupBuilder AddFormRequestFilter(this RouteGroupBuilder group)
    {
        EndpointFilterExtensions.AddEndpointFilter<FormRequestEndpointFilter>(group);
        return group;
    }
}
