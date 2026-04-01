namespace SimpleModule.Agents.Guardrails;

public interface IAgentGuardrail
{
    Task<GuardrailResult> ValidateAsync(
        string content,
        GuardrailDirection direction,
        CancellationToken cancellationToken = default
    );
}
