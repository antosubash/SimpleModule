using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SimpleModule.Cli.Commands.Skill;

public sealed record FetchedSkill(IReadOnlyList<FetchedSkillFile> Files, string ComputedHash);

public sealed class FetchedSkillFile
{
    public FetchedSkillFile(string relativePath, byte[] content)
    {
        RelativePath = relativePath;
        _content = content;
    }

    private readonly byte[] _content;

    public string RelativePath { get; }

    public byte[] GetContent() => _content;

    public int Length => _content.Length;
}

public sealed class SkillFetcher
{
    private const string GitHubApiBase = "https://api.github.com";
    private const string UserAgent = "SimpleModule.Cli";

    private readonly HttpClient _http;

    public SkillFetcher(HttpClient? http = null)
    {
        _http = http ?? new HttpClient();
        if (_http.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            _http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        }

        if (_http.DefaultRequestHeaders.Accept.Count == 0)
        {
            _http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        }

        var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        if (!string.IsNullOrWhiteSpace(token) && _http.DefaultRequestHeaders.Authorization is null)
        {
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    }

    public async Task<FetchedSkill> FetchAsync(
        SkillSource source,
        CancellationToken cancellationToken = default
    )
    {
        return source.Type switch
        {
            SkillSourceType.GitHub => await FetchGitHubAsync(source, cancellationToken)
                .ConfigureAwait(false),
            SkillSourceType.Local => FetchLocal(source),
            SkillSourceType.Scaffold => throw new InvalidOperationException(
                "Scaffold sources are produced inline; not fetched."
            ),
            _ => throw new NotSupportedException($"Source type {source.Type} is not supported."),
        };
    }

    private static FetchedSkill FetchLocal(SkillSource source)
    {
        var path = source.LocalPath ?? throw new InvalidOperationException("Missing local path.");
        var fullPath = Path.GetFullPath(path);

        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"Local skill source not found: {fullPath}");
        }

        var files = new List<FetchedSkillFile>();
        foreach (var file in Directory.EnumerateFiles(fullPath, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(fullPath, file)
                .Replace(Path.DirectorySeparatorChar, '/');
            var bytes = File.ReadAllBytes(file);
            files.Add(new FetchedSkillFile(relative, bytes));
        }

        return new FetchedSkill(files, ComputeHash(files));
    }

    private async Task<FetchedSkill> FetchGitHubAsync(
        SkillSource source,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrEmpty(source.Owner) || string.IsNullOrEmpty(source.Repo))
        {
            throw new InvalidOperationException("GitHub source requires owner and repo.");
        }

        var refName =
            source.Ref
            ?? await ResolveDefaultBranchAsync(source.Owner!, source.Repo!, cancellationToken)
                .ConfigureAwait(false);

        var basePath = source.Path ?? string.Empty;
        var files = new List<FetchedSkillFile>();
        await CollectFilesAsync(
                source.Owner!,
                source.Repo!,
                basePath,
                refName,
                basePath,
                files,
                cancellationToken
            )
            .ConfigureAwait(false);

        if (files.Count == 0)
        {
            throw new InvalidOperationException(
                $"No files found at github://{source.Owner}/{source.Repo}/{basePath}@{refName}"
            );
        }

        return new FetchedSkill(files, ComputeHash(files));
    }

    private async Task<string> ResolveDefaultBranchAsync(
        string owner,
        string repo,
        CancellationToken cancellationToken
    )
    {
        var url = $"{GitHubApiBase}/repos/{owner}/{repo}";
        using var response = await _http
            .GetAsync(new Uri(url, UriKind.Absolute), cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Failed to resolve default branch for {owner}/{repo}: HTTP {(int)response.StatusCode}"
            );
        }

        var doc = await response
            .Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return doc.TryGetProperty("default_branch", out var branch)
            ? branch.GetString() ?? "main"
            : "main";
    }

    private async Task CollectFilesAsync(
        string owner,
        string repo,
        string apiPath,
        string refName,
        string basePath,
        List<FetchedSkillFile> files,
        CancellationToken cancellationToken
    )
    {
        var encodedPath = string.IsNullOrEmpty(apiPath)
            ? string.Empty
            : "/" + Uri.EscapeDataString(apiPath).Replace("%2F", "/", StringComparison.Ordinal);
        var url =
            $"{GitHubApiBase}/repos/{owner}/{repo}/contents{encodedPath}"
            + $"?ref={Uri.EscapeDataString(refName)}";

        using var response = await _http
            .GetAsync(new Uri(url, UriKind.Absolute), cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Failed to fetch {url}: HTTP {(int)response.StatusCode}"
            );
        }

        var entries = await response
            .Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (entries.ValueKind == JsonValueKind.Object)
        {
            // Single file response — wrap in an array-like flow
            await ProcessEntryAsync(
                    owner,
                    repo,
                    refName,
                    basePath,
                    entries,
                    files,
                    cancellationToken
                )
                .ConfigureAwait(false);
            return;
        }

        if (entries.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException(
                $"Unexpected response shape for {url}: {entries.ValueKind}"
            );
        }

        foreach (var entry in entries.EnumerateArray())
        {
            await ProcessEntryAsync(owner, repo, refName, basePath, entry, files, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task ProcessEntryAsync(
        string owner,
        string repo,
        string refName,
        string basePath,
        JsonElement entry,
        List<FetchedSkillFile> files,
        CancellationToken cancellationToken
    )
    {
        var type = entry.GetProperty("type").GetString();
        var path = entry.GetProperty("path").GetString() ?? string.Empty;

        var relative =
            string.IsNullOrEmpty(basePath) ? path
            : path.StartsWith(basePath + "/", StringComparison.Ordinal)
                ? path[(basePath.Length + 1)..]
            : path == basePath ? Path.GetFileName(path)
            : path;

        if (string.Equals(type, "file", StringComparison.Ordinal))
        {
            var downloadUrl = entry.TryGetProperty("download_url", out var dl)
                ? dl.GetString()
                : null;
            if (string.IsNullOrEmpty(downloadUrl))
            {
                return;
            }

            var bytes = await _http
                .GetByteArrayAsync(new Uri(downloadUrl!, UriKind.Absolute), cancellationToken)
                .ConfigureAwait(false);
            files.Add(new FetchedSkillFile(relative.Replace('\\', '/'), bytes));
            return;
        }

        if (string.Equals(type, "dir", StringComparison.Ordinal))
        {
            await CollectFilesAsync(owner, repo, path, refName, basePath, files, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    public static string ComputeHash(IEnumerable<FetchedSkillFile> files)
    {
        var sb = new StringBuilder();
        foreach (var file in files.OrderBy(f => f.RelativePath, StringComparer.Ordinal))
        {
            var fileHash = SHA256.HashData(file.GetContent());
            sb.Append(file.RelativePath)
                .Append(':')
                .Append(Convert.ToHexString(fileHash))
                .Append('\n');
        }

        var combined = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(combined).ToLowerInvariant();
    }
}
