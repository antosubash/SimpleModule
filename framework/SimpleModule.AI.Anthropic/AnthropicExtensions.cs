using Anthropic.SDK;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SimpleModule.AI.Anthropic;

public static class AnthropicExtensions
{
    public static IServiceCollection AddAnthropicAI(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<AnthropicOptions>(configuration.GetSection("AI:Anthropic"));

        services.AddSingleton<IChatClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<AnthropicOptions>>().Value;
            var client = new AnthropicClient(opts.ApiKey);
            return client.Messages;
        });

        return services;
    }
}
