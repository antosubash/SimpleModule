namespace SimpleModule.Users.Contracts;

public interface IRoleAdminContracts
{
    Task<IReadOnlyList<RoleDto>> GetAllRolesAsync();
    Task<RoleDto?> GetRoleByIdAsync(string id);
    Task<RoleDto> CreateRoleAsync(string name, string? description);
    Task UpdateRoleAsync(string id, string name, string? description);
    Task DeleteRoleAsync(string id);
    Task<bool> HasUsersInRoleAsync(string id);
}
