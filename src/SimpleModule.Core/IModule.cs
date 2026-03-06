using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleModule.Core;

public interface IModule
{
    virtual void ConfigureServices(IServiceCollection services) { }
    virtual void ConfigureEndpoints(IEndpointRouteBuilder endpoints) { }
}
