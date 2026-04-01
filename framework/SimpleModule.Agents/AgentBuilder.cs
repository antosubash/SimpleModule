using SimpleModule.Core.Agents;

namespace SimpleModule.Agents;

public sealed class AgentBuilder : IAgentBuilder
{
    internal List<Type> AgentTypes { get; } = [];
    internal List<Type> ToolProviderTypes { get; } = [];

    public IAgentBuilder AddAgent<TAgent>()
        where TAgent : class, IAgentDefinition
    {
        AgentTypes.Add(typeof(TAgent));
        return this;
    }

    public IAgentBuilder AddToolProvider<TProvider>()
        where TProvider : class, IAgentToolProvider
    {
        ToolProviderTypes.Add(typeof(TProvider));
        return this;
    }
}
