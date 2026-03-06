namespace SimpleModule.Core.Validation;

public sealed record ValidationResult(bool IsValid, Dictionary<string, string[]> Errors)
{
    public static ValidationResult Success { get; } = new(true, new Dictionary<string, string[]>());

    public static ValidationResult WithErrors(Dictionary<string, string[]> errors) =>
        new(false, errors);
}
