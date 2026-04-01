using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleModule.AI.AzureOpenAI;

public static class AzureOpenAIExtensions
{
    public static IServiceCollection AddAzureOpenAI(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<AzureOpenAIOptions>(configuration.GetSection("AI:AzureOpenAI"));

        // Share a single AzureOpenAIClient instance across chat and embedding
        services.AddSingleton(sp =>
        {
            var opts =
                configuration.GetSection("AI:AzureOpenAI").Get<AzureOpenAIOptions>()
                ?? new AzureOpenAIOptions();
            return new AzureOpenAIClient(new Uri(opts.Endpoint), new ApiKeyCredential(opts.ApiKey));
        });

        services.AddSingleton<IChatClient>(sp =>
        {
            var opts =
                configuration.GetSection("AI:AzureOpenAI").Get<AzureOpenAIOptions>()
                ?? new AzureOpenAIOptions();
            return sp.GetRequiredService<AzureOpenAIClient>()
                .GetChatClient(opts.DeploymentName)
                .AsIChatClient();
        });

        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
        {
            var opts =
                configuration.GetSection("AI:AzureOpenAI").Get<AzureOpenAIOptions>()
                ?? new AzureOpenAIOptions();
            return sp.GetRequiredService<AzureOpenAIClient>()
                .GetEmbeddingClient(opts.EmbeddingDeploymentName)
                .AsIEmbeddingGenerator();
        });

        return services;
    }
}
