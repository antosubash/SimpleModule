namespace SimpleModule.Cli.Infrastructure;

public static class PagesRegistryFixer
{
    public static void AddEntry(string indexPath, string componentKey, string importPath)
    {
        if (!File.Exists(indexPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(indexPath)!);
            File.WriteAllText(indexPath, $$"""
                export const pages: Record<string, any> = {
                    "{{componentKey}}": () => import("{{importPath}}"),
                };
                """);
            return;
        }

        var content = File.ReadAllText(indexPath);
        var entry = $"    \"{componentKey}\": () => import(\"{importPath}\"),";
        var lastBrace = content.LastIndexOf('}');
        if (lastBrace < 0)
        {
            File.AppendAllText(indexPath, $"\n{entry}");
            return;
        }

        var newContent = content[..lastBrace] + entry + "\n" + content[lastBrace..];
        File.WriteAllText(indexPath, newContent);
    }
}
