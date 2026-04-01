using Anthropic.SDK;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
            var opts =
                configuration.GetSection("AI:Anthropic").Get<AnthropicOptions>()
                ?? new AnthropicOptions();
            var client = new AnthropicClient(opts.ApiKey);
            return client.Messages;
        });

        return services;
    }
}
