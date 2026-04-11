using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Core;
using SimpleModule.Core.Events;
using SimpleModule.Core.Exceptions;
using SimpleModule.Users.Contracts;
using SimpleModule.Users.Contracts.Events;

namespace SimpleModule.Users;

public sealed class UserAdminService(
    UserManager<ApplicationUser> userManager,
    UsersDbContext db,
    IEventBus eventBus
) : IUserAdminContracts
{
    public async Task<PagedResult<AdminUserDto>> GetUsersPagedAsync(
        string? search,
        int page,
        int pageSize,
        string? filterStatus = null,
        string? filterRole = null
    )
    {
        var query = userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(u =>
                (u.Email != null && EF.Functions.Like(u.Email, pattern))
                || EF.Functions.Like(u.DisplayName, pattern)
                || (u.UserName != null && EF.Functions.Like(u.UserName, pattern))
            );
        }

        // Status filter
        var now = DateTimeOffset.UtcNow;
        query = filterStatus switch
        {
            "active" => query.Where(u =>
                u.DeactivatedAt == null && (!u.LockoutEnd.HasValue || u.LockoutEnd <= now)
            ),
            "locked" => query.Where(u =>
                u.DeactivatedAt == null && u.LockoutEnd.HasValue && u.LockoutEnd > now
            ),
            "deactivated" => query.Where(u => u.DeactivatedAt != null),
            _ => query,
        };

        // Role filter
        if (!string.IsNullOrWhiteSpace(filterRole))
        {
            var roleId = await db
                .Roles.Where(r => r.Name == filterRole)
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (roleId is not null)
            {
                var userIdsInRole = db
                    .UserRoles.Where(ur => ur.RoleId == roleId)
                    .Select(ur => ur.UserId);

                query = query.Where(u => userIdsInRole.Contains(u.Id));
            }
            else
            {
                // Role doesn't exist — return empty
                query = query.Where(u => false);
            }
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        page = Math.Clamp(page, 1, Math.Max(1, totalPages));

        var users = await query
            .OrderBy(u => u.DisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Batch-load roles for all users in a single query instead of N+1
        var userIds = users.Select(u => u.Id).ToList();
        var userRoles = await db
            .UserRoles.Where(ur => userIds.Contains(ur.UserId))
            .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .ToListAsync();

        var rolesByUserId = userRoles
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name!).ToList());

        var items = users
            .Select(u => MapToAdminDto(u, rolesByUserId.GetValueOrDefault(u.Id, [])))
            .ToList();

        return new PagedResult<AdminUserDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<AdminUserDto?> GetAdminUserByIdAsync(UserId id)
    {
        var user = await userManager.FindByIdAsync(id.Value);
        if (user is null)
        {
            return null;
        }

        var roles = await userManager.GetRolesAsync(user);
        return MapToAdminDto(user, roles.ToList());
    }

    public async Task<AdminUserDto> CreateUserWithPasswordAsync(CreateAdminUserRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            EmailConfirmed = request.EmailConfirmed,
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description });
            throw new ValidationException(errors);
        }

        if (request.Roles.Count > 0)
        {
            await userManager.AddToRolesAsync(user, request.Roles);
        }

        var roles = await userManager.GetRolesAsync(user);

        await eventBus.PublishAsync(
            new UserCreatedEvent(UserId.From(user.Id), user.Email ?? string.Empty, user.DisplayName)
        );

        return MapToAdminDto(user, roles.ToList());
    }

    public async Task UpdateUserDetailsAsync(UserId id, UpdateAdminUserRequest request)
    {
        var user =
            await userManager.FindByIdAsync(id.Value) ?? throw new NotFoundException("User", id);

        user.DisplayName = request.DisplayName;
        user.Email = request.Email;
        user.UserName = request.Email;
        user.EmailConfirmed = request.EmailConfirmed;

        await userManager.UpdateAsync(user);

        await eventBus.PublishAsync(
            new UserUpdatedEvent(UserId.From(user.Id), user.Email ?? string.Empty, user.DisplayName)
        );
    }

    public async Task SetUserRolesAsync(UserId id, IEnumerable<string> roles)
    {
        var user =
            await userManager.FindByIdAsync(id.Value) ?? throw new NotFoundException("User", id);

        var newRoles = roles.ToHashSet();
        var currentRoles = (await userManager.GetRolesAsync(user)).ToHashSet();

        var toRemove = currentRoles.Except(newRoles).ToList();
        var toAdd = newRoles.Except(currentRoles).ToList();

        if (toRemove.Count > 0)
        {
            await userManager.RemoveFromRolesAsync(user, toRemove);
        }

        if (toAdd.Count > 0)
        {
            await userManager.AddToRolesAsync(user, toAdd);
        }

        var updatedRoles = await userManager.GetRolesAsync(user);
        await eventBus.PublishAsync(new UserRolesChangedEvent(id, updatedRoles.ToList()));
    }

    public async Task ResetPasswordAsync(UserId id, string newPassword)
    {
        var user =
            await userManager.FindByIdAsync(id.Value) ?? throw new NotFoundException("User", id);

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description });
            throw new ValidationException(errors);
        }
    }

    public async Task LockAccountAsync(UserId id)
    {
        var user =
            await userManager.FindByIdAsync(id.Value) ?? throw new NotFoundException("User", id);

        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
        await userManager.UpdateAsync(user);
    }

    public async Task UnlockAccountAsync(UserId id)
    {
        var user =
            await userManager.FindByIdAsync(id.Value) ?? throw new NotFoundException("User", id);

        user.LockoutEnd = null;
        user.AccessFailedCount = 0;
        await userManager.UpdateAsync(user);
    }

    public async Task DeactivateAsync(UserId id)
    {
        var user =
            await userManager.FindByIdAsync(id.Value) ?? throw new NotFoundException("User", id);

        user.DeactivatedAt = DateTimeOffset.UtcNow;
        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
        await userManager.UpdateAsync(user);
    }

    public async Task ReactivateAsync(UserId id)
    {
        var user =
            await userManager.FindByIdAsync(id.Value) ?? throw new NotFoundException("User", id);

        user.DeactivatedAt = null;
        user.LockoutEnd = null;
        user.AccessFailedCount = 0;
        await userManager.UpdateAsync(user);
    }

    public async Task ForceEmailReverificationAsync(UserId id)
    {
        var user =
            await userManager.FindByIdAsync(id.Value) ?? throw new NotFoundException("User", id);

        user.EmailConfirmed = false;
        await userManager.UpdateAsync(user);
    }

    public async Task DisableTwoFactorAsync(UserId id)
    {
        var user =
            await userManager.FindByIdAsync(id.Value) ?? throw new NotFoundException("User", id);

        await userManager.SetTwoFactorEnabledAsync(user, false);
        await userManager.ResetAuthenticatorKeyAsync(user);
    }

    private static AdminUserDto MapToAdminDto(ApplicationUser user, List<string> roles) =>
        new()
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            TwoFactorEnabled = user.TwoFactorEnabled,
            Roles = roles,
            IsLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow,
            IsDeactivated = user.DeactivatedAt.HasValue,
            AccessFailedCount = user.AccessFailedCount,
            CreatedAt = user.CreatedAt.ToString("O"),
            LastLoginAt = user.LastLoginAt?.ToString("O"),
        };
}
