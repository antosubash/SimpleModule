using SimpleModule.Users.Contracts;

namespace SimpleModule.Users;

public class UserService : IUserContracts
{
    public Task<IEnumerable<User>> GetAllUsersAsync() =>
        Task.FromResult<IEnumerable<User>>(new[]
        {
            new User { Id = 1, Name = "John Doe" },
            new User { Id = 2, Name = "Jane Smith" },
        });

    public Task<User?> GetUserByIdAsync(int id) =>
        Task.FromResult<User?>(new User { Id = id, Name = $"User {id}" });
}
