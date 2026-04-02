using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SimpleModule.Agents;

namespace SimpleModule.Tests.Shared.Agents;

public sealed class AgentTestFixture
{
    private readonly ServiceCollection _services = new();
    private ServiceProvider? _provider;

    public MockChatClient ChatClient { get; }

    public AgentTestFixture(params string[] responses)
    {
        ChatClient = new MockChatClient(responses);
        _services.AddSingleton<IChatClient>(ChatClient);
        _services.AddSingleton<IAgentRegistry>(new AgentRegistry());
        _services.AddSingleton(Options.Create(new AgentOptions()));
        _services.AddScoped<AgentChatService>();
    }

    public AgentTestFixture WithAgent(AgentRegistration registration)
    {
        var registry = (AgentRegistry)
            _services.BuildServiceProvider().GetRequiredService<IAgentRegistry>();
        registry.Register(registration);
        return this;
    }

    public AgentChatService BuildChatService()
    {
        _provider = _services.BuildServiceProvider();
        return _provider.GetRequiredService<AgentChatService>();
    }

    public void Dispose() => _provider?.Dispose();
}
