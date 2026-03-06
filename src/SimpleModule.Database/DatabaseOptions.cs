namespace SimpleModule.Database;

public sealed class DatabaseOptions
{
    public string DefaultConnection { get; set; } = string.Empty;
    public Dictionary<string, string> ModuleConnections { get; set; } = [];
}
