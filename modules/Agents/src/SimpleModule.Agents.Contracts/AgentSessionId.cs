using Vogen;

namespace SimpleModule.Agents.Contracts;

[ValueObject<string>(conversions: Conversions.SystemTextJson | Conversions.EfCoreValueConverter)]
public readonly partial struct AgentSessionId;
