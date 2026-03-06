namespace SimpleModule.Core;

public interface IModule
{
    void ConfigureServices(IServiceCollection services);
    void ConfigureEndpoints(IEndpointRouteBuilder endpoints);
}
