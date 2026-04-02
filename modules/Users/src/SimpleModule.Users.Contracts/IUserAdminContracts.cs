using SimpleModule.Core;

namespace SimpleModule.Users.Contracts;

public interface IUserAdminContracts
{
    Task<PagedResult<AdminUserDto>> GetUsersPagedAsync(
        string? search,
        int page,
        int pageSize,
        string? filterStatus = null,
        string? filterRole = null
    );
    Task<AdminUserDto?> GetAdminUserByIdAsync(UserId id);
    Task<AdminUserDto> CreateUserWithPasswordAsync(CreateAdminUserRequest request);
    Task UpdateUserDetailsAsync(UserId id, UpdateAdminUserRequest request);
    Task SetUserRolesAsync(UserId id, IEnumerable<string> roles);
    Task ResetPasswordAsync(UserId id, string newPassword);
    Task LockAccountAsync(UserId id);
    Task UnlockAccountAsync(UserId id);
    Task DeactivateAsync(UserId id);
    Task ReactivateAsync(UserId id);
    Task ForceEmailReverificationAsync(UserId id);
    Task DisableTwoFactorAsync(UserId id);
}
