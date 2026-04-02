using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

        services.AddSingleton(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<OpenAIOptions>>().Value;
            return new OpenAIClient(opts.ApiKey);
        });

        services.AddSingleton<IChatClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<OpenAIOptions>>().Value;
            return sp.GetRequiredService<OpenAIClient>().GetChatClient(opts.Model).AsIChatClient();
        });

        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<OpenAIOptions>>().Value;
            return sp.GetRequiredService<OpenAIClient>()
                .GetEmbeddingClient(opts.EmbeddingModel)
                .AsIEmbeddingGenerator();
        });

        return services;
    }
}
