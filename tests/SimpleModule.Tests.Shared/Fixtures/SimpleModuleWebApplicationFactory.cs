using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SimpleModule.Tests.Shared.Fixtures;

public class SimpleModuleWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Swap services for testing:
        // builder.ConfigureServices(services =>
        // {
        //     services.AddScoped<IUserContracts, FakeUserContracts>();
        // });
    }
}
