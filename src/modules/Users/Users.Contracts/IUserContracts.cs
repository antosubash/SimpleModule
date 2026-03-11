namespace SimpleModule.Users.Contracts;

public interface IUserContracts
{
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task<UserDto?> GetUserByIdAsync(string id);
    Task<UserDto?> GetCurrentUserAsync(string userId);
}
