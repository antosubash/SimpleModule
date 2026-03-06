using Microsoft.EntityFrameworkCore;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users;

public class UserService(UsersDbContext db) : IUserContracts
{
    public async Task<IEnumerable<User>> GetAllUsersAsync() => await db.Users.ToListAsync();

    public async Task<User?> GetUserByIdAsync(int id) => await db.Users.FindAsync(id);
}
