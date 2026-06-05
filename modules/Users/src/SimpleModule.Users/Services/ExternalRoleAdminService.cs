using SimpleModule.Core;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users.Services;

#pragma warning disable CA1812 // Instantiated via DI
[ManualContractRegistration]
internal sealed class ExternalRoleAdminService : IRoleAdminContracts
{
    public Task<IReadOnlyList<RoleDto>> GetAllRolesAsync()
    {
        return Task.FromResult<IReadOnlyList<RoleDto>>([]);
    }

    public Task<RoleDto?> GetRoleByIdAsync(string id)
    {
        return Task.FromResult<RoleDto?>(null);
    }

    public Task<RoleDto> CreateRoleAsync(string name, string? description)
    {
        throw new NotSupportedException(
            "Role management is handled by the external identity provider."
        );
    }

    public Task UpdateRoleAsync(string id, string name, string? description)
    {
        throw new NotSupportedException(
            "Role management is handled by the external identity provider."
        );
    }

    public Task DeleteRoleAsync(string id)
    {
        throw new NotSupportedException(
            "Role management is handled by the external identity provider."
        );
    }

    public Task<bool> HasUsersInRoleAsync(string id)
    {
        return Task.FromResult(false);
    }
}
