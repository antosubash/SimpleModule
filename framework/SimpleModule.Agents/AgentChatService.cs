using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SimpleModule.Agents.Dtos;
using SimpleModule.Core.Agents;
using SimpleModule.Rag;

namespace SimpleModule.Agents;

public sealed class AgentChatService(
    IAgentRegistry registry,
    IChatClient chatClient,
    IServiceProvider serviceProvider,
    IOptions<AgentOptions> agentOptions
)
{
    public async Task<AgentChatResponse> ChatAsync(
        string agentName,
        AgentChatRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var registration =
            registry.GetByName(agentName)
            ?? throw new InvalidOperationException($"Agent '{agentName}' not found");

        var agentDef = (IAgentDefinition)
            ActivatorUtilities.CreateInstance(serviceProvider, registration.AgentDefinitionType);

        var messages = new List<ChatMessage> { new(ChatRole.System, agentDef.Instructions) };

        // Inject RAG context if available and enabled
        var options = agentOptions.Value;
        var enableRag = agentDef.EnableRag ?? options.EnableRag;
        if (enableRag)
        {
            var ragPipeline = serviceProvider.GetService<IRagPipeline>();
            if (ragPipeline is not null)
            {
                var ragResult = await ragPipeline.QueryAsync(
                    request.Message,
                    cancellationToken: cancellationToken
                );
                if (ragResult.Sources.Count > 0)
                {
                    var contextText = string.Join(
                        "\n\n",
                        ragResult.Sources.Select(s => $"### {s.Title}\n{s.Content}")
                    );
                    messages.Add(
                        new ChatMessage(ChatRole.System, $"## Retrieved Knowledge\n\n{contextText}")
                    );
                }
            }
        }

        // Resolve tools from tool providers
        var tools = new List<AITool>();
        using var scope = serviceProvider.CreateScope();
        foreach (var providerType in registration.ToolProviderTypes)
        {
            var provider = (IAgentToolProvider)
                ActivatorUtilities.CreateInstance(scope.ServiceProvider, providerType);

            var methods = providerType
                .GetMethods()
                .Where(m => m.GetCustomAttributes(typeof(AgentToolAttribute), false).Length > 0);

            foreach (var method in methods)
            {
                tools.Add(AIFunctionFactory.Create(method, provider));
            }
        }

        messages.Add(new ChatMessage(ChatRole.User, request.Message));

        var chatOptions = new ChatOptions
        {
            MaxOutputTokens = agentDef.MaxTokens ?? options.MaxTokens,
            Temperature = agentDef.Temperature ?? options.Temperature,
            Tools = tools.Count > 0 ? tools : null,
        };

        var response = await chatClient.GetResponseAsync(messages, chatOptions, cancellationToken);

        var sessionId = request.SessionId ?? Guid.NewGuid().ToString();

        return new AgentChatResponse(response.Text ?? "", sessionId);
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(
        string agentName,
        AgentChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var registration =
            registry.GetByName(agentName)
            ?? throw new InvalidOperationException($"Agent '{agentName}' not found");

        var agentDef = (IAgentDefinition)
            ActivatorUtilities.CreateInstance(serviceProvider, registration.AgentDefinitionType);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, agentDef.Instructions),
            new(ChatRole.User, request.Message),
        };

        var options = agentOptions.Value;
        var chatOptions = new ChatOptions
        {
            MaxOutputTokens = agentDef.MaxTokens ?? options.MaxTokens,
            Temperature = agentDef.Temperature ?? options.Temperature,
        };

        await foreach (
            var update in chatClient.GetStreamingResponseAsync(
                messages,
                chatOptions,
                cancellationToken
            )
        )
        {
            foreach (var content in update.Contents)
            {
                if (content is TextContent textContent && textContent.Text is not null)
                {
                    yield return textContent.Text;
                }
            }
        }
    }
}
