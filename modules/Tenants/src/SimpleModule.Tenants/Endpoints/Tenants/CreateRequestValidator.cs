using System.Text.RegularExpressions;
using SimpleModule.Core.Validation;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.Endpoints.Tenants;

public static partial class CreateRequestValidator
{
    public static ValidationResult Validate(CreateTenantRequest request) =>
        new ValidationBuilder()
            .AddErrorIf(
                string.IsNullOrWhiteSpace(request.Name),
                "Name",
                "Tenant name is required."
            )
            .AddErrorIf(
                string.IsNullOrWhiteSpace(request.Slug),
                "Slug",
                "Tenant slug is required."
            )
            .AddErrorIf(
                !string.IsNullOrWhiteSpace(request.Slug) && !SlugPattern().IsMatch(request.Slug),
                "Slug",
                "Slug must contain only lowercase letters, numbers, and hyphens."
            )
            .AddErrorIf(
                !string.IsNullOrWhiteSpace(request.AdminEmail)
                    && !request.AdminEmail.Contains('@', StringComparison.Ordinal),
                "AdminEmail",
                "Invalid email format."
            )
            .Build();

    [GeneratedRegex(@"^[a-z0-9][a-z0-9-]*[a-z0-9]$|^[a-z0-9]$")]
    private static partial Regex SlugPattern();
}
