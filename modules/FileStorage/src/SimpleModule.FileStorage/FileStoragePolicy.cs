using System.Security.Claims;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Authorization.Policies;
using SimpleModule.Core.Extensions;
using SimpleModule.FileStorage.Contracts;

namespace SimpleModule.FileStorage;

/// <summary>
/// Instance-level rules for stored files: the uploader (or an admin) may view, download,
/// or delete a file. Ownership denials use <c>DenyAsNotFound</c> so a caller holding the
/// coarse <c>FileStorage.View</c>/<c>Delete</c> permission cannot probe which file IDs
/// belong to other users. Auto-registered by the source generator.
/// </summary>
public sealed class FileStoragePolicy : IPolicy<StoredFile>
{
    public Task<AuthorizationResult> AuthorizeAsync(
        ClaimsPrincipal user,
        string action,
        StoredFile resource,
        CancellationToken cancellationToken = default
    )
    {
        var result = action switch
        {
            PolicyActions.View or PolicyActions.Delete => AllowOwnerOrAdmin(user, resource),
            _ => AuthorizationResult.Deny($"Unknown file action '{action}'."),
        };
        return Task.FromResult(result);
    }

    private static AuthorizationResult AllowOwnerOrAdmin(ClaimsPrincipal user, StoredFile file)
    {
        if (user.IsInRole(WellKnownRoles.Admin))
        {
            return AuthorizationResult.Allow();
        }

        var userId = user.GetUserId();
        return userId is not null && file.CreatedByUserId == userId
            ? AuthorizationResult.Allow()
            : AuthorizationResult.DenyAsNotFound("You can only access your own files.");
    }
}
