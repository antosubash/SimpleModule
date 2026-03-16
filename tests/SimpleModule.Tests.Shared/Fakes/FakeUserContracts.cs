using System.Globalization;
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

    private int _nextId = 100;

    public Task<UserDto> CreateUserAsync(CreateUserRequest request)
    {
        var user = new UserDto
        {
            Id = (_nextId++).ToString(CultureInfo.InvariantCulture),
            Email = request.Email,
            DisplayName = request.DisplayName,
            EmailConfirmed = false,
            TwoFactorEnabled = false,
        };
        Users.Add(user);
        return Task.FromResult(user);
    }

    public Task<UserDto> UpdateUserAsync(string id, UpdateUserRequest request)
    {
        var user = Users.FirstOrDefault(u => u.Id == id);
        if (user is null)
        {
            throw new SimpleModule.Core.Exceptions.NotFoundException("User", id);
        }

        user.Email = request.Email;
        user.DisplayName = request.DisplayName;
        return Task.FromResult(user);
    }

    public Task DeleteUserAsync(string id)
    {
        var user = Users.FirstOrDefault(u => u.Id == id);
        if (user is null)
        {
            throw new SimpleModule.Core.Exceptions.NotFoundException("User", id);
        }

        Users.Remove(user);
        return Task.CompletedTask;
    }
}
