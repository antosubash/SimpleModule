namespace SimpleModule.Agents.Module;

public sealed class AgentSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string AgentName { get; set; } = "";
    public string? UserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastMessageAt { get; set; } = DateTimeOffset.UtcNow;
}
