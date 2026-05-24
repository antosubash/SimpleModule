using System.Security.Claims;

namespace SimpleModule.Core.FormRequests;

public abstract class FormRequest
{
    public virtual bool Authorize(ClaimsPrincipal user) => true;

    public virtual void Prepare() { }

    internal abstract Task<FluentValidation.Results.ValidationResult> ValidateAsync(
        CancellationToken cancellationToken
    );
}
