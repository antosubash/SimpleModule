using SimpleModule.Core;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Services;

#pragma warning disable CA1812 // Instantiated via DI
[ManualContractRegistration]
internal sealed class ExternalUserAdminService : IUserAdminContracts
{
    public Task<PagedResult<AdminUserDto>> GetUsersPagedAsync(
        string? search,
        int page,
        int pageSize,
        string? filterStatus = null,
        string? filterRole = null
    )
    {
        return Task.FromResult(
            new PagedResult<AdminUserDto>
            {
                Items = [],
                TotalCount = 0,
                Page = page,
                PageSize = pageSize,
            }
        );
    }

    public Task<AdminUserDto?> GetAdminUserByIdAsync(UserId id)
    {
        return Task.FromResult<AdminUserDto?>(null);
    }

    public Task<AdminUserDto> CreateUserWithPasswordAsync(CreateAdminUserRequest request)
    {
        throw new NotSupportedException(
            "User management is handled by the external identity provider."
        );
    }

    public Task UpdateUserDetailsAsync(UserId id, UpdateAdminUserRequest request)
    {
        throw new NotSupportedException(
            "User management is handled by the external identity provider."
        );
    }

    public Task SetUserRolesAsync(UserId id, IEnumerable<string> roles)
    {
        throw new NotSupportedException(
            "User management is handled by the external identity provider."
        );
    }

    public Task ResetPasswordAsync(UserId id, string newPassword)
    {
        throw new NotSupportedException(
            "User management is handled by the external identity provider."
        );
    }

    public Task LockAccountAsync(UserId id)
    {
        throw new NotSupportedException(
            "User management is handled by the external identity provider."
        );
    }

    public Task UnlockAccountAsync(UserId id)
    {
        throw new NotSupportedException(
            "User management is handled by the external identity provider."
        );
    }

    public Task DeactivateAsync(UserId id)
    {
        throw new NotSupportedException(
            "User management is handled by the external identity provider."
        );
    }

    public Task ReactivateAsync(UserId id)
    {
        throw new NotSupportedException(
            "User management is handled by the external identity provider."
        );
    }

    public Task ForceEmailReverificationAsync(UserId id)
    {
        throw new NotSupportedException(
            "User management is handled by the external identity provider."
        );
    }

    public Task ForcePhoneReverificationAsync(UserId id)
    {
        throw new NotSupportedException(
            "User management is handled by the external identity provider."
        );
    }

    public Task DisableTwoFactorAsync(UserId id)
    {
        throw new NotSupportedException(
            "User management is handled by the external identity provider."
        );
    }
}
