namespace SimpleModule.Users.Contracts;

public interface IUserContracts
{
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<User?> GetUserByIdAsync(int id);
}
