using SimpleModule.Users.Contracts;

namespace SimpleModule.Tests.Shared.Fakes;

public class FakeUserContracts : IUserContracts
{
    public List<User> Users { get; set; } = FakeDataGenerators.UserFaker.Generate(3);

    public Task<IEnumerable<User>> GetAllUsersAsync() => Task.FromResult<IEnumerable<User>>(Users);

    public Task<User?> GetUserByIdAsync(int id) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Id == id));
}
