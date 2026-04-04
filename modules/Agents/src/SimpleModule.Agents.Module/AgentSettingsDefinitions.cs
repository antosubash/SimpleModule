using SimpleModule.Core.Settings;

namespace SimpleModule.Agents.Module;

public static class AgentSettingsDefinitions
{
    public static void Register(ISettingsBuilder settings)
    {
        settings
            .Add(
                new SettingDefinition
                {
                    Key = "Agents.Enabled",
                    DisplayName = "Enable AI Agents",
                    Description = "Global kill switch for all AI agent endpoints",
                    Group = "AI Agents",
                    Type = SettingType.Bool,
                    Scope = SettingScope.Application,
                    DefaultValue = "true",
                }
            )
            .Add(
                new SettingDefinition
                {
                    Key = "Agents.MaxTokens",
                    DisplayName = "Max Tokens",
                    Description = "Default maximum tokens per agent response",
                    Group = "AI Agents",
                    Type = SettingType.Number,
                    Scope = SettingScope.Application,
                    DefaultValue = "4096",
                }
            )
            .Add(
                new SettingDefinition
                {
                    Key = "Agents.Temperature",
                    DisplayName = "Temperature",
                    Description = "Default temperature for agent responses (0.0-2.0)",
                    Group = "AI Agents",
                    Type = SettingType.Number,
                    Scope = SettingScope.Application,
                    DefaultValue = "0.7",
                }
            )
            .Add(
                new SettingDefinition
                {
                    Key = "Agents.EnableRag",
                    DisplayName = "Enable RAG Context",
                    Description = "Whether agents use retrieval-augmented generation for context",
                    Group = "AI Agents",
                    Type = SettingType.Bool,
                    Scope = SettingScope.Application,
                    DefaultValue = "true",
                }
            )
            .Add(
                new SettingDefinition
                {
                    Key = "Agents.EnableStreaming",
                    DisplayName = "Enable Streaming",
                    Description = "Enable SSE streaming endpoints for real-time responses",
                    Group = "AI Agents",
                    Type = SettingType.Bool,
                    Scope = SettingScope.Application,
                    DefaultValue = "true",
                }
            )
            .Add(
                new SettingDefinition
                {
                    Key = "Agents.RateLimit.RequestsPerMinute",
                    DisplayName = "Rate Limit (requests/min)",
                    Description = "Maximum agent requests per user per minute",
                    Group = "AI Agents",
                    Type = SettingType.Number,
                    Scope = SettingScope.Application,
                    DefaultValue = "60",
                }
            )
            .Add(
                new SettingDefinition
                {
                    Key = "Agents.RateLimit.TokensPerMinute",
                    DisplayName = "Token Limit (tokens/min)",
                    Description = "Maximum tokens per user per minute",
                    Group = "AI Agents",
                    Type = SettingType.Number,
                    Scope = SettingScope.Application,
                    DefaultValue = "100000",
                }
            );
    }
}
