namespace SimpleModule.Core.Agents;

/// <summary>
/// Fluent builder for manual agent registration in <see cref="IModule.ConfigureAgents"/>.
/// Used as an escape hatch when auto-discovery is insufficient.
/// </summary>
public interface IAgentBuilder
{
    IAgentBuilder AddAgent<TAgent>()
        where TAgent : class, IAgentDefinition;
    IAgentBuilder AddToolProvider<TProvider>()
        where TProvider : class, IAgentToolProvider;
}
