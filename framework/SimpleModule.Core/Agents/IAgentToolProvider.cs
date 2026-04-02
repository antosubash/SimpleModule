namespace SimpleModule.Core.Agents;

/// <summary>
/// Marker interface for classes that expose <see cref="AgentToolAttribute"/> methods.
/// Resolved from DI so it can inject module contracts, DbContext, etc.
/// </summary>
#pragma warning disable CA1040 // Avoid empty interfaces - marker interface by design
public interface IAgentToolProvider;
#pragma warning restore CA1040
