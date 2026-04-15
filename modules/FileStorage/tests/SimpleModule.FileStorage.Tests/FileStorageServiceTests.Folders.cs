using FluentAssertions;

namespace SimpleModule.FileStorage.Tests;

public sealed partial class FileStorageServiceTests
{
    [Fact]
    public async Task GetFilesAsync_Filters_By_Folder()
    {
        using var s1 = new MemoryStream("a"u8.ToArray());
        using var s2 = new MemoryStream("b"u8.ToArray());
        using var s3 = new MemoryStream("c"u8.ToArray());

        await _service.UploadFileAsync(s1, "root.txt", "text/plain");
        await _service.UploadFileAsync(s2, "in-folder.txt", "text/plain", "docs");
        await _service.UploadFileAsync(s3, "also-in-folder.txt", "text/plain", "docs");

        var rootFiles = await _service.GetFilesAsync();
        rootFiles.Should().HaveCount(1);

        var docsFiles = await _service.GetFilesAsync("docs");
        docsFiles.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetFilesAsync_Returns_Empty_For_Nonexistent_Folder()
    {
        var result = await _service.GetFilesAsync("nonexistent");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFilesAsync_Does_Not_Return_Files_From_Subfolders()
    {
        using var s1 = new MemoryStream("a"u8.ToArray());
        using var s2 = new MemoryStream("b"u8.ToArray());

        await _service.UploadFileAsync(s1, "top.txt", "text/plain", "docs");
        await _service.UploadFileAsync(s2, "nested.txt", "text/plain", "docs/sub");

        var docsFiles = await _service.GetFilesAsync("docs");
        docsFiles.Should().HaveCount(1);
        docsFiles.First().FileName.Should().Be("top.txt");
    }

    [Fact]
    public async Task GetFilesAsync_Returns_Files_Ordered_By_Name()
    {
        using var s1 = new MemoryStream("a"u8.ToArray());
        using var s2 = new MemoryStream("b"u8.ToArray());
        using var s3 = new MemoryStream("c"u8.ToArray());

        await _service.UploadFileAsync(s3, "charlie.txt", "text/plain");
        await _service.UploadFileAsync(s1, "alpha.txt", "text/plain");
        await _service.UploadFileAsync(s2, "bravo.txt", "text/plain");

        var files = (await _service.GetFilesAsync()).ToList();

        files.Select(f => f.FileName).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetFoldersAsync_Returns_Top_Level_Folders()
    {
        using var s1 = new MemoryStream("a"u8.ToArray());
        using var s2 = new MemoryStream("b"u8.ToArray());
        using var s3 = new MemoryStream("c"u8.ToArray());

        await _service.UploadFileAsync(s1, "a.txt", "text/plain", "docs");
        await _service.UploadFileAsync(s2, "b.txt", "text/plain", "images");
        await _service.UploadFileAsync(s3, "c.txt", "text/plain", "images/thumbs");

        var folders = (await _service.GetFoldersAsync()).ToList();

        folders.Should().BeEquivalentTo(["docs", "images"]);
    }

    [Fact]
    public async Task GetFoldersAsync_Returns_Child_Folders()
    {
        using var s1 = new MemoryStream("a"u8.ToArray());
        using var s2 = new MemoryStream("b"u8.ToArray());
        using var s3 = new MemoryStream("c"u8.ToArray());

        await _service.UploadFileAsync(s1, "a.txt", "text/plain", "docs/reports");
        await _service.UploadFileAsync(s2, "b.txt", "text/plain", "docs/invoices");
        await _service.UploadFileAsync(s3, "c.txt", "text/plain", "images");

        var folders = (await _service.GetFoldersAsync("docs")).ToList();

        folders.Should().BeEquivalentTo(["docs/invoices", "docs/reports"]);
    }

    [Fact]
    public async Task GetFoldersAsync_Returns_Empty_For_Leaf_Folder()
    {
        using var stream = new MemoryStream("a"u8.ToArray());
        await _service.UploadFileAsync(stream, "a.txt", "text/plain", "docs");

        var folders = (await _service.GetFoldersAsync("docs")).ToList();

        folders.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFoldersAsync_Returns_Empty_When_No_Files()
    {
        var folders = (await _service.GetFoldersAsync()).ToList();

        folders.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFoldersAsync_Deduplicates_Nested_Folders()
    {
        using var s1 = new MemoryStream("a"u8.ToArray());
        using var s2 = new MemoryStream("b"u8.ToArray());
        using var s3 = new MemoryStream("c"u8.ToArray());

        await _service.UploadFileAsync(s1, "a.txt", "text/plain", "docs/reports/2024");
        await _service.UploadFileAsync(s2, "b.txt", "text/plain", "docs/reports/2025");
        await _service.UploadFileAsync(s3, "c.txt", "text/plain", "docs/invoices");

        var folders = (await _service.GetFoldersAsync("docs")).ToList();

        // Should return "docs/reports" and "docs/invoices", not "docs/reports/2024" etc.
        folders.Should().BeEquivalentTo(["docs/invoices", "docs/reports"]);
    }
}
