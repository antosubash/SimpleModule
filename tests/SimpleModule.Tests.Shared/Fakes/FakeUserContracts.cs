using SimpleModule.Users.Contracts;

namespace SimpleModule.Tests.Shared.Fakes;

public class FakeUserContracts : IUserContracts
{
    public List<UserDto> Users { get; set; } = FakeDataGenerators.UserFaker.Generate(3);

    public Task<IEnumerable<UserDto>> GetAllUsersAsync() =>
        Task.FromResult<IEnumerable<UserDto>>(Users);

    public Task<UserDto?> GetUserByIdAsync(string id) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Id == id));

    public Task<UserDto?> GetCurrentUserAsync(string userId) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Id == userId));
}
