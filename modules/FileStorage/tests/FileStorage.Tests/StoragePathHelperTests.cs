using FluentAssertions;
using SimpleModule.Storage;

namespace SimpleModule.FileStorage.Tests;

public sealed class StoragePathHelperTests
{
    [Theory]
    [InlineData("foo/bar.txt", "foo/bar.txt")]
    [InlineData("/foo/bar.txt", "foo/bar.txt")]
    [InlineData("foo/bar.txt/", "foo/bar.txt")]
    [InlineData("/foo/bar.txt/", "foo/bar.txt")]
    [InlineData("foo\\bar.txt", "foo/bar.txt")]
    [InlineData("\\foo\\bar.txt\\", "foo/bar.txt")]
    [InlineData("  foo/bar.txt  ", "foo/bar.txt")]
    [InlineData("simple.txt", "simple.txt")]
    public void Normalize_Produces_Clean_Path(string input, string expected)
    {
        StoragePathHelper.Normalize(input).Should().Be(expected);
    }

    [Theory]
    [InlineData(null, "file.txt", "file.txt")]
    [InlineData("", "file.txt", "file.txt")]
    [InlineData("  ", "file.txt", "file.txt")]
    [InlineData("docs", "file.txt", "docs/file.txt")]
    [InlineData("docs/reports", "file.txt", "docs/reports/file.txt")]
    [InlineData("/docs/", "file.txt", "docs/file.txt")]
    public void Combine_Joins_Folder_And_FileName(string? folder, string fileName, string expected)
    {
        StoragePathHelper.Combine(folder, fileName).Should().Be(expected);
    }

    [Theory]
    [InlineData("docs/reports/file.txt", "file.txt")]
    [InlineData("file.txt", "file.txt")]
    [InlineData("/docs/file.txt", "file.txt")]
    public void GetFileName_Extracts_Name(string path, string expected)
    {
        StoragePathHelper.GetFileName(path).Should().Be(expected);
    }

    [Theory]
    [InlineData("docs/reports/file.txt", "docs/reports")]
    [InlineData("docs/file.txt", "docs")]
    [InlineData("file.txt", null)]
    [InlineData("/docs/file.txt", "docs")]
    public void GetFolder_Extracts_Folder(string path, string? expected)
    {
        StoragePathHelper.GetFolder(path).Should().Be(expected);
    }
}
