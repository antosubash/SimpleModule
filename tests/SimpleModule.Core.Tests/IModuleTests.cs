using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;

namespace SimpleModule.Core.Tests;

public class IModuleTests
{
    private sealed class EmptyModule : IModule { }

    private sealed class TestModule : IModule
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<TestService>();
        }

        public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/test", () => "hello");
        }
    }

    private sealed class TestService { }

    [Fact]
    public void DefaultMethods_DoNotThrow()
    {
        IModule module = new EmptyModule();
        var services = new ServiceCollection();

        var act = () => module.ConfigureServices(services);

        act.Should().NotThrow();
    }

    [Fact]
    public void ConcreteModule_ConfigureServices_RegistersExpectedServices()
    {
        var module = new TestModule();
        var services = new ServiceCollection();

        module.ConfigureServices(services);

        services.Should().Contain(sd => sd.ServiceType == typeof(TestService));
    }

    [Fact]
    public void ConcreteModule_ConfigureEndpoints_MapsExpectedRoutes()
    {
        var module = new TestModule();
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        var act = () => module.ConfigureEndpoints(app);

        act.Should().NotThrow();
    }
}
