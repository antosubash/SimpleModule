using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Database;
using SimpleModule.Users.Contracts;
using SimpleModule.Users.Features.GetAllUsers;
using SimpleModule.Users.Features.GetUserById;

namespace SimpleModule.Users;

[Module("Users")]
public class UsersModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<UsersDbContext>(configuration, "Users");
        services.AddScoped<IUserContracts, UserService>();
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/users");
        GetAllUsersEndpoint.Map(group);
        GetUserByIdEndpoint.Map(group);
    }
}
