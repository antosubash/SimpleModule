namespace SimpleModule.Agents.Guardrails;

public sealed class PromptInjectionGuardrail : IAgentGuardrail
{
    private static readonly string[] SuspiciousPatterns =
    [
        "ignore previous instructions",
        "ignore all previous",
        "disregard previous",
        "forget your instructions",
        "you are now",
        "new instructions:",
        "system prompt:",
        "ignore the above",
        "override your",
    ];

    public Task<GuardrailResult> ValidateAsync(
        string content,
        GuardrailDirection direction,
        CancellationToken cancellationToken = default
    )
    {
        if (direction != GuardrailDirection.Input)
            return Task.FromResult(GuardrailResult.Allowed());

        foreach (var pattern in SuspiciousPatterns)
        {
            if (content.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(
                    GuardrailResult.Blocked(
                        $"Input contains suspicious pattern that may be a prompt injection attempt"
                    )
                );
            }
        }

        return Task.FromResult(GuardrailResult.Allowed());
    }
}
