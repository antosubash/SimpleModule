using Vogen;

namespace SimpleModule.Permissions.Contracts;

[ValueObject<string>(conversions: Conversions.SystemTextJson | Conversions.EfCoreValueConverter)]
public readonly partial struct RoleId
{
    private static Vogen.Validation Validate(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? Vogen.Validation.Invalid("RoleId cannot be empty")
            : Vogen.Validation.Ok;
}
