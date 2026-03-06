using System.Text.Json.Serialization;
using SimpleModule.Core;

namespace SimpleModule.Users;

[Module("Users")]
public class UsersModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/users");

        group.MapGet(
            "/",
            async (IUserService userService) =>
            {
                var users = await userService.GetAllUsersAsync();
                return Results.Ok(users);
            }
        );

        group.MapGet(
            "/{id}",
            async (int id, IUserService userService) =>
            {
                var user = await userService.GetUserByIdAsync(id);
                return user is not null ? Results.Ok(user) : Results.NotFound();
            }
        );
    }
}

public interface IUserService
{
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<User?> GetUserByIdAsync(int id);
}

public class UserService : IUserService
{
    public Task<IEnumerable<User>> GetAllUsersAsync()
    {
        // In a real application, this would fetch from a database
        return Task.FromResult<IEnumerable<User>>(
            new[]
            {
                new User { Id = 1, Name = "John Doe" },
                new User { Id = 2, Name = "Jane Smith" },
            }
        );
    }

    public Task<User?> GetUserByIdAsync(int id)
    {
        // In a real application, this would fetch from a database
        return Task.FromResult<User?>(new User { Id = id, Name = $"User {id}" });
    }
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

[JsonSerializable(typeof(User))]
[JsonSerializable(typeof(IEnumerable<User>))]
public partial class UsersJsonContext : JsonSerializerContext;
