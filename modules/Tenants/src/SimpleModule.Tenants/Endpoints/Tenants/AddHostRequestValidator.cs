using SimpleModule.Core.Validation;
using SimpleModule.Tenants.Contracts;

namespace SimpleModule.Tenants.Endpoints.Tenants;

public static class AddHostRequestValidator
{
    public static ValidationResult Validate(AddTenantHostRequest request) =>
        new ValidationBuilder()
            .AddErrorIf(
                string.IsNullOrWhiteSpace(request.HostName),
                "HostName",
                "Host name is required."
            )
            .AddErrorIf(
                !string.IsNullOrWhiteSpace(request.HostName) && request.HostName.Length > 512,
                "HostName",
                "Host name must not exceed 512 characters."
            )
            .Build();
}
