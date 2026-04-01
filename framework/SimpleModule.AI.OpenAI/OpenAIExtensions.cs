using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;

namespace SimpleModule.AI.OpenAI;

public static class OpenAIExtensions
{
    public static IServiceCollection AddOpenAI(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<OpenAIOptions>(configuration.GetSection("AI:OpenAI"));

        services.AddSingleton<IChatClient>(sp =>
        {
            var opts =
                configuration.GetSection("AI:OpenAI").Get<OpenAIOptions>() ?? new OpenAIOptions();
            return new OpenAIClient(opts.ApiKey).GetChatClient(opts.Model).AsIChatClient();
        });

        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
        {
            var opts =
                configuration.GetSection("AI:OpenAI").Get<OpenAIOptions>() ?? new OpenAIOptions();
            return new OpenAIClient(opts.ApiKey)
                .GetEmbeddingClient(opts.EmbeddingModel)
                .AsIEmbeddingGenerator();
        });

        return services;
    }
}
