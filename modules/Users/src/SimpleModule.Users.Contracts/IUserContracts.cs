namespace SimpleModule.Users.Contracts;

public interface IUserContracts
{
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task<UserDto?> GetUserByIdAsync(UserId id);
    Task<UserDto?> GetCurrentUserAsync(UserId userId);
    Task<UserDto> CreateUserAsync(CreateUserRequest request);
    Task<UserDto> UpdateUserAsync(UserId id, UpdateUserRequest request);
    Task DeleteUserAsync(UserId id);
    Task<IReadOnlyDictionary<string, string>> GetRoleIdsByNamesAsync(IEnumerable<string> roleNames);
}
