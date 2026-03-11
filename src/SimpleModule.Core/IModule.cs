using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleModule.Core;

public interface IModule
{
    virtual void ConfigureServices(IServiceCollection services, IConfiguration configuration) { }
    virtual void ConfigureEndpoints(IEndpointRouteBuilder endpoints) { }
}
