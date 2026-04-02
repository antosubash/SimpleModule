using SimpleModule.Core.Validation;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.Endpoints.Tenants;

public static class UpdateRequestValidator
{
    public static ValidationResult Validate(UpdateTenantRequest request) =>
        new ValidationBuilder()
            .AddErrorIf(string.IsNullOrWhiteSpace(request.Name), "Name", "Tenant name is required.")
            .AddErrorIf(
                !string.IsNullOrWhiteSpace(request.AdminEmail)
                    && !request.AdminEmail.Contains('@', StringComparison.Ordinal),
                "AdminEmail",
                "Invalid email format."
            )
            .Build();
}
