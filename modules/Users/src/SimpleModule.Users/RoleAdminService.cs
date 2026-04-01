using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Core.Exceptions;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users;

public sealed class RoleAdminService(
    RoleManager<ApplicationRole> roleManager,
    UserManager<ApplicationUser> userManager
) : IRoleAdminContracts
{
    public async Task<IReadOnlyList<RoleDto>> GetAllRolesAsync()
    {
        var roles = await roleManager.Roles.OrderBy(r => r.Name).ToListAsync();

        var result = new List<RoleDto>(roles.Count);
        foreach (var role in roles)
        {
            var usersInRole = await userManager.GetUsersInRoleAsync(role.Name!);
            result.Add(MapToDto(role, usersInRole.Count));
        }

        return result;
    }

    public async Task<RoleDto?> GetRoleByIdAsync(string id)
    {
        var role = await roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return null;
        }

        var usersInRole = await userManager.GetUsersInRoleAsync(role.Name!);
        return MapToDto(role, usersInRole.Count);
    }

    public async Task<RoleDto> CreateRoleAsync(string name, string? description)
    {
        var trimmedName = name.Trim();
        if (string.IsNullOrEmpty(trimmedName))
        {
            throw new ValidationException(
                new Dictionary<string, string[]> { ["Name"] = ["Role name is required."] }
            );
        }

        var role = new ApplicationRole
        {
            Name = trimmedName,
            Description = description?.Trim() is { Length: > 0 } d ? d : null,
        };

        var result = await roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            var errors = result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description });
            throw new ValidationException(errors);
        }

        return MapToDto(role, 0);
    }

    public async Task UpdateRoleAsync(string id, string name, string? description)
    {
        var role = await roleManager.FindByIdAsync(id) ?? throw new NotFoundException("Role", id);

        role.Name = name.Trim();
        role.Description = description?.Trim() is { Length: > 0 } d ? d : null;
        await roleManager.UpdateAsync(role);
    }

    public async Task DeleteRoleAsync(string id)
    {
        var role = await roleManager.FindByIdAsync(id) ?? throw new NotFoundException("Role", id);

        var usersInRole = await userManager.GetUsersInRoleAsync(role.Name!);
        if (usersInRole.Count > 0)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["Role"] = ["Cannot delete a role that has users assigned to it."],
                }
            );
        }

        await roleManager.DeleteAsync(role);
    }

    public async Task<bool> HasUsersInRoleAsync(string id)
    {
        var role = await roleManager.FindByIdAsync(id) ?? throw new NotFoundException("Role", id);

        var usersInRole = await userManager.GetUsersInRoleAsync(role.Name!);
        return usersInRole.Count > 0;
    }

    private static RoleDto MapToDto(ApplicationRole role, int userCount) =>
        new()
        {
            Id = role.Id,
            Name = role.Name ?? string.Empty,
            Description = role.Description,
            UserCount = userCount,
            CreatedAt = role.CreatedAt.ToString("O"),
        };
}
