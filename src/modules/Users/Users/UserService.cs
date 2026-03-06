using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users;

public partial class UserService(UsersDbContext db, ILogger<UserService> logger) : IUserContracts
{
    public async Task<IEnumerable<User>> GetAllUsersAsync() => await db.Users.ToListAsync();

    public async Task<User?> GetUserByIdAsync(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null)
        {
            LogUserNotFound(logger, id);
        }

        return user;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "User with ID {UserId} not found")]
    private static partial void LogUserNotFound(ILogger logger, int userId);
}
