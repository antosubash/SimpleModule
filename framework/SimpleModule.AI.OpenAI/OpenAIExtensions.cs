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

        // Share a single OpenAIClient instance across chat and embedding
        services.AddSingleton(sp =>
        {
            var opts =
                configuration.GetSection("AI:OpenAI").Get<OpenAIOptions>() ?? new OpenAIOptions();
            return new OpenAIClient(opts.ApiKey);
        });

        services.AddSingleton<IChatClient>(sp =>
        {
            var opts =
                configuration.GetSection("AI:OpenAI").Get<OpenAIOptions>() ?? new OpenAIOptions();
            return sp.GetRequiredService<OpenAIClient>().GetChatClient(opts.Model).AsIChatClient();
        });

        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
        {
            var opts =
                configuration.GetSection("AI:OpenAI").Get<OpenAIOptions>() ?? new OpenAIOptions();
            return sp.GetRequiredService<OpenAIClient>()
                .GetEmbeddingClient(opts.EmbeddingModel)
                .AsIEmbeddingGenerator();
        });

        return services;
    }
}
