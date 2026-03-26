namespace SimpleModule.Cli.Infrastructure;

public static class NpmWorkspaceFixer
{
    public static void AddWorkspaceGlob(string packageJsonPath, string glob)
    {
        var content = File.ReadAllText(packageJsonPath);
        var workspacesStart = content.IndexOf("\"workspaces\"", StringComparison.Ordinal);
        if (workspacesStart >= 0)
        {
            var arrayStart = content.IndexOf('[', workspacesStart);
            var arrayEnd = content.IndexOf(']', arrayStart);
            if (arrayStart >= 0 && arrayEnd >= 0)
            {
                var entry = $"\"{glob}\"";
                var existing = content[arrayStart..arrayEnd].Trim('[').Trim();
                var separator = existing.Length > 0 ? ", " : "";
                var newContent = content[..arrayEnd] + separator + entry + content[arrayEnd..];
                File.WriteAllText(packageJsonPath, newContent);
                return;
            }
        }

        var lastBrace = content.LastIndexOf('}');
        if (lastBrace >= 0)
        {
            var insert = $",\n  \"workspaces\": [\"{glob}\"]";
            File.WriteAllText(packageJsonPath, content[..lastBrace] + insert + "\n}");
        }
    }
}
