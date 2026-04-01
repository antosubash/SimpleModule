namespace SimpleModule.Agents;

public sealed class AgentOptions
{
    public bool Enabled { get; set; } = true;
    public int MaxTokens { get; set; } = 4096;
    public float Temperature { get; set; } = 0.7f;
    public bool EnableRag { get; set; } = true;
    public bool EnableStreaming { get; set; } = true;
    public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromMinutes(30);
    public AgentRateLimitOptions RateLimit { get; set; } = new();
}

public sealed class AgentRateLimitOptions
{
    public int RequestsPerMinute { get; set; } = 60;
    public int TokensPerMinute { get; set; } = 100_000;
}
