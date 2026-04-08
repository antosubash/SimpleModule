using System.Security.Claims;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Extensions;
using SimpleModule.FileStorage.Contracts;

namespace SimpleModule.FileStorage.Endpoints.Files;

internal static class FileOwnershipCheck
{
    internal static bool CanAccess(StoredFile file, ClaimsPrincipal user) =>
        user.IsInRole(WellKnownRoles.Admin) || file.CreatedByUserId == user.GetUserId();
}
