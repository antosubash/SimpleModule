using SimpleModule.Agents.Guardrails;

namespace SimpleModule.Agents.Module;

public sealed class ContentLengthGuardrail(
    int maxInputLength = 10_000,
    int maxOutputLength = 50_000
) : IAgentGuardrail
{
    public Task<GuardrailResult> ValidateAsync(
        string content,
        GuardrailDirection direction,
        CancellationToken cancellationToken = default
    )
    {
        var maxLength = direction == GuardrailDirection.Input ? maxInputLength : maxOutputLength;
        if (content.Length > maxLength)
        {
            return Task.FromResult(
                GuardrailResult.Blocked(
                    $"Content exceeds maximum length of {maxLength} characters ({content.Length} provided)"
                )
            );
        }

        return Task.FromResult(GuardrailResult.Allowed());
    }
}
