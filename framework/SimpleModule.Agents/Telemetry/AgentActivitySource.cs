using System.Diagnostics;

namespace SimpleModule.Agents.Telemetry;

public static class AgentActivitySource
{
    public const string Name = "SimpleModule.Agents";
    public static readonly ActivitySource Instance = new(Name, "1.0.0");

    public static Activity? StartAgentInvocation(string agentName) =>
        Instance
            .StartActivity("agent.invoke", ActivityKind.Internal)
            ?.SetTag("agent.name", agentName);

    public static Activity? StartToolCall(string agentName, string toolName) =>
        Instance
            .StartActivity("agent.tool.call", ActivityKind.Internal)
            ?.SetTag("agent.name", agentName)
            .SetTag("tool.name", toolName);

    public static Activity? StartLlmCall(string agentName) =>
        Instance
            .StartActivity("agent.llm.call", ActivityKind.Client)
            ?.SetTag("agent.name", agentName);

    public static Activity? StartRagSearch(string agentName) =>
        Instance
            .StartActivity("agent.rag.search", ActivityKind.Internal)
            ?.SetTag("agent.name", agentName);
}
