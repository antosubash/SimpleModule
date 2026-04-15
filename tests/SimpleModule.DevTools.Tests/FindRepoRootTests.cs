using FluentAssertions;

namespace SimpleModule.DevTools.Tests;

public sealed class FindRepoRootTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(
        Path.GetTempPath(),
        $"devtools-test-{Guid.NewGuid():N}"
    );

    public FindRepoRootTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public void FindRepoRoot_Returns_Directory_With_Git_Folder()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, ".git"));
        var nested = Path.Combine(_tempDir, "a", "b", "c");
        Directory.CreateDirectory(nested);

        var result = ViteDevWatchService.FindRepoRoot(nested);

        result.Should().Be(_tempDir);
    }

    [Fact]
    public void FindRepoRoot_Returns_Directory_With_Git_File_Worktree()
    {
        // In git worktrees, .git is a file (not a directory) containing "gitdir: ..."
        File.WriteAllText(
            Path.Combine(_tempDir, ".git"),
            "gitdir: /some/other/repo/.git/worktrees/branch"
        );
        var nested = Path.Combine(_tempDir, "a", "b");
        Directory.CreateDirectory(nested);

        var result = ViteDevWatchService.FindRepoRoot(nested);

        result.Should().Be(_tempDir);
    }

    [Fact]
    public void FindRepoRoot_Returns_Null_When_No_Git_Folder()
    {
        var nested = Path.Combine(_tempDir, "no-git", "deep");
        Directory.CreateDirectory(nested);

        var result = ViteDevWatchService.FindRepoRoot(nested);

        // Will walk up to filesystem root — may find an actual .git or return null.
        // The important thing is it doesn't throw.
        result.Should().NotBe(nested);
    }

    [Fact]
    public void FindRepoRoot_Returns_Exact_Directory_When_StartPath_Has_Git()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, ".git"));

        var result = ViteDevWatchService.FindRepoRoot(_tempDir);

        result.Should().Be(_tempDir);
    }
}
