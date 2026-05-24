using System.Security.Claims;
using FluentValidation.Results;

namespace SimpleModule.Core.FormRequests;

public abstract class FormRequest
{
    public virtual bool Authorize(ClaimsPrincipal user) => true;

    public virtual void Prepare() { }

    public async Task<ValidationResult> ValidateRulesAsync(
        CancellationToken cancellationToken = default
    ) => await ValidateAsync(cancellationToken);

    internal abstract Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken);
}
