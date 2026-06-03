using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleModule.DevTools.Tests;

public sealed class MapLiveReloadTests
{
    [Fact]
    public async Task MapLiveReload_Endpoint_IsAnonymous()
    {
        // The framework authenticates by default, so the live-reload WebSocket must
        // opt out — otherwise the handshake gets a 302 to login and the browser
        // client reconnects forever, flooding the console (#233).
        var builder = WebApplication.CreateSlimBuilder();
        builder.Services.AddSingleton<LiveReloadServer>();
        await using var app = builder.Build();

        app.MapLiveReload();

        var endpoint = ((IEndpointRouteBuilder)app)
            .DataSources.SelectMany(ds => ds.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(e => e.RoutePattern.RawText == "/dev/live-reload");

        endpoint
            .Metadata.GetMetadata<IAllowAnonymous>()
            .Should()
            .NotBeNull("the dev live-reload socket must not be behind the auth fallback policy");
    }
}
