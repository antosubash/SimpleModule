using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleModule.Users.Contracts;
using SimpleModule.Users.Contracts.Events;
using Wolverine;

namespace SimpleModule.Users.Services;

#pragma warning disable CA1812 // Instantiated via DI
internal sealed partial class ExternalUserService(
    UsersDbContext db,
    IMessageBus bus,
    ILogger<ExternalUserService> logger
) : IUserContracts
{
    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        return await db.Set<ApplicationUser>().Select(u => MapToDto(u)).ToListAsync();
    }

    public async Task<UserDto?> GetUserByIdAsync(UserId id)
    {
        var user = await db.Set<ApplicationUser>().FindAsync(id.Value);
        return user is null ? null : MapToDto(user);
    }

    public async Task<UserDto?> GetCurrentUserAsync(UserId userId)
    {
        return await GetUserByIdAsync(userId);
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
    {
        var user = new ApplicationUser
        {
            Id = request.Id ?? Guid.NewGuid().ToString(),
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
        };

        db.Set<ApplicationUser>().Add(user);
        await db.SaveChangesAsync();

        LogUserCreated(logger, user.Id, user.Email);
        await bus.PublishAsync(
            new UserCreatedEvent(UserId.From(user.Id), user.Email ?? string.Empty, user.DisplayName)
        );

        return MapToDto(user);
    }

    public async Task<UserDto> UpdateUserAsync(UserId id, UpdateUserRequest request)
    {
        var user =
            await db.Set<ApplicationUser>().FindAsync(id.Value)
            ?? throw new Core.Exceptions.NotFoundException("User", id);

        user.Email = request.Email;
        user.UserName = request.Email;
        user.DisplayName = request.DisplayName;

        await db.SaveChangesAsync();

        LogUserUpdated(logger, user.Id);
        await bus.PublishAsync(
            new UserUpdatedEvent(UserId.From(user.Id), user.Email ?? string.Empty, user.DisplayName)
        );

        return MapToDto(user);
    }

    public async Task DeleteUserAsync(UserId id)
    {
        var user =
            await db.Set<ApplicationUser>().FindAsync(id.Value)
            ?? throw new Core.Exceptions.NotFoundException("User", id);

        db.Set<ApplicationUser>().Remove(user);
        await db.SaveChangesAsync();

        LogUserDeleted(logger, id);
        await bus.PublishAsync(new UserDeletedEvent(id));
    }

    public async Task<IReadOnlyDictionary<string, string>> GetRoleIdsByNamesAsync(
        IEnumerable<string> roleNames
    )
    {
        var names = roleNames as ICollection<string> ?? roleNames.ToList();
        return await db.Set<ApplicationRole>()
            .Where(r => names.Contains(r.Name!))
            .ToDictionaryAsync(r => r.Name!, r => r.Id);
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
