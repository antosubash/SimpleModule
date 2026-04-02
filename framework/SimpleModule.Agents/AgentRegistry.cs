namespace SimpleModule.Agents;

public sealed class AgentRegistry : IAgentRegistry
{
    private readonly List<AgentRegistration> _agents = [];
    private readonly Dictionary<string, AgentRegistration> _byName = new(
        StringComparer.OrdinalIgnoreCase
    );

    public IReadOnlyList<AgentRegistration> GetAll() => _agents;

    public AgentRegistration? GetByName(string name) => _byName.GetValueOrDefault(name);

    public void Register(AgentRegistration registration)
    {
        _agents.Add(registration);
        _byName[registration.Name] = registration;
    }
}
