using System.Text.RegularExpressions;
using SimpleModule.Agents.Guardrails;

namespace SimpleModule.Agents.Module;

public sealed partial class PiiRedactionGuardrail : IAgentGuardrail
{
    public Task<GuardrailResult> ValidateAsync(
        string content,
        GuardrailDirection direction,
        CancellationToken cancellationToken = default
    )
    {
        var sanitized = content;
        sanitized = EmailRegex().Replace(sanitized, "[EMAIL_REDACTED]");
        sanitized = PhoneRegex().Replace(sanitized, "[PHONE_REDACTED]");
        sanitized = SsnRegex().Replace(sanitized, "[SSN_REDACTED]");

        if (sanitized != content)
            return Task.FromResult(GuardrailResult.Sanitized(sanitized));

        return Task.FromResult(GuardrailResult.Allowed());
    }

    [GeneratedRegex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b", RegexOptions.Compiled)]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled)]
    private static partial Regex SsnRegex();
}
