using Vogen;

namespace SimpleModule.Users.Contracts;

[ValueObject<string>(conversions: Conversions.SystemTextJson | Conversions.EfCoreValueConverter)]
public readonly partial struct UserId
{
    private static Vogen.Validation Validate(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? Vogen.Validation.Invalid("UserId cannot be empty")
            : Vogen.Validation.Ok;
}
