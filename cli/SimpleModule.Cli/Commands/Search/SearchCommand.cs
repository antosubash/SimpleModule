using System.ComponentModel;
using System.Text.Json;
using SimpleModule.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.Search;

public sealed class SearchSettings : CommandSettings
{
    [CommandArgument(0, "[query]")]
    [Description("Search text. Empty lists all SimpleModule modules on the registry.")]
    public string? Query { get; init; }

    [CommandOption("--source")]
    [Description(
        "Registry to search: NuGet V3 service index URL or a local folder feed. Default: sm.json registry."
    )]
    public string? Source { get; init; }

    [CommandOption("--take")]
    [Description("Maximum results. Default: 10")]
    public int Take { get; init; } = 10;

    [CommandOption("--prerelease")]
    [Description("Include prerelease packages.")]
    public bool Prerelease { get; init; }
}

/// <summary>
/// Searches a registry for SimpleModule modules (the `simplemodule-module`
/// tag). Local folder feeds are scanned by reading each nupkg's manifest;
/// remote registries go through the NuGet search API. When run inside a host
/// solution, each result shows framework compatibility with that host.
/// </summary>
public sealed class SearchCommand : AsyncCommand<SearchSettings>
{
    private static readonly HttpClient SharedHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
    };

    public override async Task<int> ExecuteAsync(CommandContext context, SearchSettings settings)
    {
        var solution = SolutionContext.Discover();
        var hostVersion = solution is null
            ? null
            : HostFrameworkVersionResolver.Resolve(solution.RootPath);

        var source =
            settings.Source
            ?? SmConfig.Load(solution?.RootPath ?? Directory.GetCurrentDirectory()).Registry;

        var results = NuGetClient.IsLocalDirectorySource(source)
            ? SearchLocalFeed(Path.GetFullPath(source), settings)
            : await SearchRegistryAsync(source, settings);

        if (results.Count == 0)
        {
            AnsiConsole.MarkupLine(
                $"[yellow]No SimpleModule modules found on {Markup.Escape(source)}"
                    + (
                        string.IsNullOrEmpty(settings.Query)
                            ? ""
                            : $" for '{Markup.Escape(settings.Query)}'"
                    )
                    + ".[/]"
            );
            return 0;
        }

        var table = new Table().RoundedBorder();
        table.AddColumn("Package");
        table.AddColumn("Version");
        table.AddColumn("Description");
        if (hostVersion is not null)
        {
            table.AddColumn("Compat");
        }

        foreach (var result in results.Take(settings.Take))
        {
            var row = new List<string>
            {
                $"[blue]{Markup.Escape(result.Id)}[/]",
                Markup.Escape(result.Version),
                Markup.Escape(Truncate(result.Description, 60)),
            };
            if (hostVersion is not null)
            {
                row.Add(CompatCell(result.FrameworkCompat, hostVersion));
            }

            table.AddRow([.. row]);
        }

        AnsiConsole.Write(table);
        if (hostVersion is not null)
        {
            AnsiConsole.MarkupLine(
                $"[dim]Compat evaluated against host framework {Markup.Escape(hostVersion)}.[/]"
            );
        }

        return 0;
    }

    private sealed record SearchResult(
        string Id,
        string Version,
        string Description,
        string? FrameworkCompat
    );

    private static List<SearchResult> SearchLocalFeed(string feedDir, SearchSettings settings)
    {
        var results = new List<SearchResult>();
        if (!Directory.Exists(feedDir))
        {
            return results;
        }

        // Highest version per package id, manifest-bearing packages only.
        var byId = new Dictionary<string, (string Version, string Path)>(
            StringComparer.OrdinalIgnoreCase
        );
        foreach (var nupkg in Directory.EnumerateFiles(feedDir, "*.nupkg"))
        {
            var name = Path.GetFileNameWithoutExtension(nupkg);
            var split = SplitIdAndVersion(name);
            if (split is null)
            {
                continue;
            }

            var (id, version) = split.Value;
            if (!settings.Prerelease && SemVerStringComparer.IsPrerelease(version))
            {
                continue;
            }

            if (
                !byId.TryGetValue(id, out var existing)
                || SemVerStringComparer.Instance.Compare(version, existing.Version) > 0
            )
            {
                byId[id] = (version, nupkg);
            }
        }

        foreach (var (id, entry) in byId.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
        {
            var manifest = NupkgManifestReader.TryRead(entry.Path, id);
            if (manifest is null)
            {
                continue; // not a SimpleModule module
            }

            if (
                !string.IsNullOrEmpty(settings.Query)
                && !id.Contains(settings.Query, StringComparison.OrdinalIgnoreCase)
                && !manifest.DisplayName.Contains(
                    settings.Query,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                continue;
            }

            results.Add(
                new SearchResult(id, entry.Version, manifest.DisplayName, manifest.FrameworkCompat)
            );
        }

        return results;
    }

    private static async Task<List<SearchResult>> SearchRegistryAsync(
        string serviceIndexUrl,
        SearchSettings settings
    )
    {
        var searchBase = await ResolveSearchServiceAsync(new Uri(serviceIndexUrl));
        // Include the tag in the server-side query so tagged modules rank into
        // the fetched window; the client-side tag filter below stays the gate.
        var query = Uri.EscapeDataString(("simplemodule-module " + (settings.Query ?? "")).Trim());
        var url =
            $"{searchBase}?q={query}&take={Math.Max(settings.Take * 3, 30)}"
            + $"&prerelease={(settings.Prerelease ? "true" : "false")}";

        using var response = await SharedHttpClient.GetAsync(new Uri(url));
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var results = new List<SearchResult>();
        foreach (var item in doc.RootElement.GetProperty("data").EnumerateArray())
        {
            // Only SimpleModule modules: the tag convention is the registry contract.
            var tags = item.TryGetProperty("tags", out var tagsEl)
                ? tagsEl.EnumerateArray().Select(t => t.GetString() ?? "").ToList()
                : [];
            if (!tags.Contains("simplemodule-module", StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            results.Add(
                new SearchResult(
                    item.GetProperty("id").GetString() ?? "",
                    item.GetProperty("version").GetString() ?? "",
                    item.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                    // The manifest's compat range lives inside the nupkg; avoid N
                    // downloads at search time. Compat is shown from the manifest
                    // for local feeds and verified definitively by `sm add`.
                    FrameworkCompat: null
                )
            );
        }

        return results;
    }

    private static async Task<string> ResolveSearchServiceAsync(Uri serviceIndexUrl)
    {
        using var response = await SharedHttpClient.GetAsync(serviceIndexUrl);
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        foreach (var resource in doc.RootElement.GetProperty("resources").EnumerateArray())
        {
            var type = resource.GetProperty("@type").GetString() ?? "";
            if (type.StartsWith("SearchQueryService", StringComparison.Ordinal))
            {
                return resource.GetProperty("@id").GetString() ?? "";
            }
        }

        throw new InvalidOperationException(
            $"Registry '{serviceIndexUrl}' exposes no SearchQueryService resource."
        );
    }

    private static string CompatCell(string? frameworkCompat, string hostVersion)
    {
        if (frameworkCompat is null)
        {
            return "[dim]install-time check[/]";
        }

        var compat = FrameworkCompatChecker.Check(frameworkCompat, hostVersion);
        return compat.Compatible
            ? $"[green]✓[/] {Markup.Escape(frameworkCompat)}"
            : $"[red]✗[/] {Markup.Escape(frameworkCompat)}";
    }

    /// <summary>Splits "Some.Package.1.2.3-pre" into id and version.</summary>
    public static (string Id, string Version)? SplitIdAndVersion(string fileName)
    {
        var parts = fileName.Split('.');
        for (var i = 1; i < parts.Length; i++)
        {
            var version = string.Join('.', parts[i..]);
            // The version must look like "N.N..."; a digit-leading id segment
            // (e.g. Acme.2FA) must not be mistaken for the version start.
            if (System.Text.RegularExpressions.Regex.IsMatch(version, @"^\d+\.\d+"))
            {
                var id = string.Join('.', parts[..i]);
                if (id.Length > 0)
                {
                    return (id, version);
                }
            }
        }

        return null;
    }

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..(max - 1)] + "…";
}
