using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SimpleModule.AI.Ollama;

public static class OllamaExtensions
{
    public static IServiceCollection AddOllamaAI(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<OllamaOptions>(configuration.GetSection("AI:Ollama"));

        services.AddSingleton<IChatClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
            return new OllamaChatClient(new Uri(opts.Endpoint), opts.Model);
        });

        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
            return new OllamaEmbeddingGenerator(new Uri(opts.Endpoint), opts.EmbeddingModel);
        });

        return services;
    }
}
