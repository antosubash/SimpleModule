namespace SimpleModule.Settings;

public sealed class SettingValidationException : Exception
{
    public string Key { get; } = string.Empty;
    public IReadOnlyList<string> Errors { get; } = [];

    public SettingValidationException() { }

    public SettingValidationException(string message)
        : base(message) { }

    public SettingValidationException(string message, Exception innerException)
        : base(message, innerException) { }

    public SettingValidationException(string key, IReadOnlyList<string> errors)
        : base($"Validation failed for setting '{key}': {string.Join("; ", errors)}")
    {
        Key = key;
        Errors = errors;
    }
}
