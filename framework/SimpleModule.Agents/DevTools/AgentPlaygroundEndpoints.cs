using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Agents.Dtos;

namespace SimpleModule.Agents.DevTools;

public static class AgentPlaygroundEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/dev/agents")
            .WithTags("DevTools")
            .ExcludeFromDescription()
            .AllowAnonymous();

        group.MapGet(
            "/",
            (IAgentRegistry registry) =>
            {
                var agents = registry
                    .GetAll()
                    .Select(a => new AgentInfo(a.Name, a.Description, a.ModuleName))
                    .ToList();
                return Results.Ok(new { title = "Agent Playground", agents });
            }
        );

        group.MapGet(
            "/{name}/info",
            (string name, IAgentRegistry registry) =>
            {
                var agent = registry.GetByName(name);
                if (agent is null)
                    return Results.NotFound();

                return Results.Ok(
                    new
                    {
                        agent.Name,
                        agent.Description,
                        agent.ModuleName,
                        agentType = agent.AgentDefinitionType.FullName,
                        toolProviders = agent.ToolProviderTypes.Select(t => t.FullName).ToList(),
                    }
                );
            }
        );

        group.MapGet(
            "/health",
            (IAgentRegistry registry) =>
                Results.Ok(new { registeredAgents = registry.GetAll().Count, status = "ok" })
        );
    }
}
