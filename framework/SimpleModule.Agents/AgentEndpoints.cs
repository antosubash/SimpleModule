using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Agents.Dtos;

namespace SimpleModule.Agents;

public static class AgentEndpoints
{
    public static void MapAgentEndpoints(IEndpointRouteBuilder app, IAgentRegistry registry)
    {
        var group = app.MapGroup("/api/agents").WithTags("Agents").RequireAuthorization();

        group.MapGet(
            "/",
            (IAgentRegistry reg) =>
                reg.GetAll().Select(a => new AgentInfo(a.Name, a.Description, a.ModuleName))
        );

        group.MapPost(
            "/{name}/chat",
            async (
                string name,
                AgentChatRequest request,
                AgentChatService service,
                CancellationToken ct
            ) =>
            {
                var response = await service.ChatAsync(name, request, ct);
                return Results.Ok(response);
            }
        );

        group.MapPost(
            "/{name}/chat/stream",
            async (
                string name,
                AgentChatRequest request,
                AgentChatService service,
                HttpContext httpContext,
                CancellationToken ct
            ) =>
            {
                httpContext.Response.ContentType = "text/event-stream";
                httpContext.Response.Headers.CacheControl = "no-cache";
                httpContext.Response.Headers.Connection = "keep-alive";

                await foreach (var chunk in service.ChatStreamAsync(name, request, ct))
                {
                    var data = System.Text.Json.JsonSerializer.Serialize(new { text = chunk });
                    await httpContext.Response.WriteAsync($"data: {data}\n\n", ct);
                    await httpContext.Response.Body.FlushAsync(ct);
                }

                await httpContext.Response.WriteAsync("data: [DONE]\n\n", ct);
            }
        );

        group
            .MapPost(
                "/{name}/chat/structured",
                async (
                    string name,
                    AgentChatRequest request,
                    AgentChatService service,
                    CancellationToken ct
                ) =>
                {
                    var response = await service.ChatAsync(name, request, ct);
                    return Results.Ok(response);
                }
            )
            .WithDescription("Chat with structured JSON output");
    }
}
