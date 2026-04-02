namespace SimpleModule.Agents;

public sealed record AgentRegistration(
    string Name,
    string Description,
    string ModuleName,
    Type AgentDefinitionType,
    IReadOnlyList<Type> ToolProviderTypes
);
