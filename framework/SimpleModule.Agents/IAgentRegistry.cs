namespace SimpleModule.Agents;

public interface IAgentRegistry
{
    IReadOnlyList<AgentRegistration> GetAll();
    AgentRegistration? GetByName(string name);
}
