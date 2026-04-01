namespace SimpleModule.Core.Agents;

/// <summary>
/// Marks a method on an <see cref="IAgentToolProvider"/> as a callable tool for agents.
/// The source generator discovers these and registers them via AIFunctionFactory at runtime.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class AgentToolAttribute : Attribute
{
    /// <summary>Optional tool name. Defaults to the method name if not specified.</summary>
    public string? Name { get; set; }

    /// <summary>Description of what the tool does, used by the LLM to decide when to call it.</summary>
    public string? Description { get; set; }
}
