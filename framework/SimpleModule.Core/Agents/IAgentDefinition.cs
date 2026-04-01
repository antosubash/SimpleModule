namespace SimpleModule.Core.Agents;

/// <summary>
/// Defines an AI agent. Discovered by the source generator and auto-registered.
/// </summary>
public interface IAgentDefinition
{
    /// <summary>Unique agent name used in endpoint URLs (e.g., "product-search").</summary>
    string Name { get; }

    /// <summary>Human-readable description of what this agent does.</summary>
    string Description { get; }

    /// <summary>System instructions/prompt for the agent.</summary>
    string Instructions { get; }

    /// <summary>Optional max tokens override. Returns null to use global default.</summary>
    virtual int? MaxTokens => null;

    /// <summary>Optional temperature override. Returns null to use global default.</summary>
    virtual float? Temperature => null;

    /// <summary>Optional RAG override. Returns null to use global default.</summary>
    virtual bool? EnableRag => null;
}
