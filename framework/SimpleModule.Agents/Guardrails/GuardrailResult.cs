namespace SimpleModule.Agents.Guardrails;

public sealed record GuardrailResult(
    bool IsAllowed,
    string? Reason = null,
    string? SanitizedContent = null
)
{
    public static GuardrailResult Allowed() => new(true);

    public static GuardrailResult Blocked(string reason) => new(false, reason);

    public static GuardrailResult Sanitized(string sanitizedContent) =>
        new(true, SanitizedContent: sanitizedContent);
}
