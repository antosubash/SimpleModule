namespace SimpleModule.AI.Anthropic;

public sealed class AnthropicOptions
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "claude-sonnet-4-20250514";
}
