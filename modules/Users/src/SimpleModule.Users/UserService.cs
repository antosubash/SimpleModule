using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleModule.Core.Events;
using SimpleModule.Users.Contracts;
using SimpleModule.Users.Contracts.Events;

namespace SimpleModule.Users;

public partial class UserService(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IEventBus eventBus,
    ILogger<UserService> logger
) : IUserContracts
{
    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        var users = await userManager.Users.ToListAsync();
        return users.Select(MapToDto);
    }

    public async Task<UserDto?> GetUserByIdAsync(UserId id)
    {
        var user = await userManager.FindByIdAsync(id.Value);
        if (user is null)
        {
            LogUserNotFound(logger, id);
            return null;
        }

        return MapToDto(user);
    }

    public async Task<UserDto?> GetCurrentUserAsync(UserId userId)
    {
        return await GetUserByIdAsync(userId);
    }

    private static UserDto MapToDto(ApplicationUser user) =>
        new()
        {
            Id = UserId.From(user.Id),
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            EmailConfirmed = user.EmailConfirmed,
            TwoFactorEnabled = user.TwoFactorEnabled,
        };

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description });
            throw new Core.Exceptions.ValidationException(errors);
        }

        LogUserCreated(logger, user.Id, user.Email);

        await eventBus.PublishAsync(
            new UserCreatedEvent(UserId.From(user.Id), user.Email ?? string.Empty, user.DisplayName)
        );

        return MapToDto(user);
    }

    public async Task<UserDto> UpdateUserAsync(UserId id, UpdateUserRequest request)
    {
        var user = await userManager.FindByIdAsync(id.Value);
        if (user is null)
        {
            throw new Core.Exceptions.NotFoundException("User", id);
        }

        user.Email = request.Email;
        user.UserName = request.Email;
        user.DisplayName = request.DisplayName;

        await userManager.UpdateAsync(user);

        LogUserUpdated(logger, user.Id);

        await eventBus.PublishAsync(
            new UserUpdatedEvent(UserId.From(user.Id), user.Email ?? string.Empty, user.DisplayName)
        );

        return MapToDto(user);
    }

    public async Task DeleteUserAsync(UserId id)
    {
        var user = await userManager.FindByIdAsync(id.Value);
        if (user is null)
        {
            throw new Core.Exceptions.NotFoundException("User", id);
        }

        await userManager.DeleteAsync(user);

        LogUserDeleted(logger, id);

        eventBus.PublishInBackground(new UserDeletedEvent(id));
    }

    public async Task<IReadOnlyDictionary<string, string>> GetRoleIdsByNamesAsync(
        IEnumerable<string> roleNames
    )
    {
        var nameCollection = roleNames as ICollection<string> ?? roleNames.ToList();
        var roles = await roleManager
            .Roles.Where(r => nameCollection.Contains(r.Name!))
            .ToDictionaryAsync(r => r.Name!, r => r.Id);
        return roles;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "User with ID {UserId} not found")]
    private static partial void LogUserNotFound(ILogger logger, UserId userId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "User {UserId} created with email {Email}"
    )]
    private static partial void LogUserCreated(ILogger logger, string userId, string email);

    [LoggerMessage(Level = LogLevel.Information, Message = "User {UserId} updated")]
    private static partial void LogUserUpdated(ILogger logger, string userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "User {UserId} deleted")]
    private static partial void LogUserDeleted(ILogger logger, UserId userId);
}
