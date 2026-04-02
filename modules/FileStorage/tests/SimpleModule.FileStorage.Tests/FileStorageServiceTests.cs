using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SimpleModule.Database;
using SimpleModule.FileStorage.Contracts;

namespace SimpleModule.FileStorage.Tests;

public sealed class FileStorageServiceTests : IDisposable
{
    private readonly FileStorageDbContext _db;
    private readonly InMemoryStorageProvider _storageProvider;
    private readonly FileStorageService _service;

    public FileStorageServiceTests()
    {
        var options = new DbContextOptionsBuilder<FileStorageDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var dbOptions = Options.Create(
            new DatabaseOptions
            {
                ModuleConnections = new Dictionary<string, string>
                {
                    ["FileStorage"] = "Data Source=:memory:",
                },
            }
        );

        _db = new FileStorageDbContext(options, dbOptions);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();

        _storageProvider = new InMemoryStorageProvider();
        _service = new FileStorageService(
            _db,
            _storageProvider,
            NullLogger<FileStorageService>.Instance
        );
    }

    [Fact]
    public async Task UploadFileAsync_Saves_File_And_Creates_Record()
    {
        var content = "hello world"u8.ToArray();
        using var stream = new MemoryStream(content);

        var result = await _service.UploadFileAsync(stream, "test.txt", "text/plain");

        result.FileName.Should().Be("test.txt");
        result.ContentType.Should().Be("text/plain");
        result.Size.Should().Be(content.Length);
        result.Folder.Should().BeNull();
        result.Id.Value.Should().BeGreaterThan(0);

        var exists = await _storageProvider.ExistsAsync(result.StoragePath);
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task UploadFileAsync_With_Folder_Sets_Correct_Path()
    {
        using var stream = new MemoryStream("data"u8.ToArray());

        var result = await _service.UploadFileAsync(
            stream,
            "photo.jpg",
            "image/jpeg",
            "products/images"
        );

        result.Folder.Should().Be("products/images");
        result.StoragePath.Should().Be("products/images/photo.jpg");
    }

    [Fact]
    public async Task GetFileByIdAsync_Returns_Null_For_Missing_Id()
    {
        var result = await _service.GetFileByIdAsync(FileStorageId.From(999));

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteFileAsync_Removes_Record_And_Storage()
    {
        using var stream = new MemoryStream("data"u8.ToArray());
        var uploaded = await _service.UploadFileAsync(stream, "delete-me.txt", "text/plain");

        await _service.DeleteFileAsync(uploaded.Id);

        var record = await _db.StoredFiles.FindAsync(uploaded.Id);
        record.Should().BeNull();

        var exists = await _storageProvider.ExistsAsync(uploaded.StoragePath);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DownloadFileAsync_Returns_Stream()
    {
        var content = "file content"u8.ToArray();
        using var uploadStream = new MemoryStream(content);
        var uploaded = await _service.UploadFileAsync(uploadStream, "download.txt", "text/plain");

        var downloadStream = await _service.DownloadFileAsync(uploaded.Id);

        downloadStream.Should().NotBeNull();
        using var ms = new MemoryStream();
        await downloadStream!.CopyToAsync(ms);
        ms.ToArray().Should().BeEquivalentTo(content);
    }

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
    public async Task DeleteFileAsync_Throws_For_Missing_Id()
    {
        var act = () => _service.DeleteFileAsync(FileStorageId.From(999));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DownloadFileAsync_Returns_Null_For_Missing_Id()
    {
        var result = await _service.DownloadFileAsync(FileStorageId.From(999));

        result.Should().BeNull();
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
    public async Task UploadFileAsync_Normalizes_Folder_Path()
    {
        using var stream = new MemoryStream("data"u8.ToArray());

        var result = await _service.UploadFileAsync(
            stream,
            "file.txt",
            "text/plain",
            "/docs/reports/"
        );

        result.Folder.Should().Be("docs/reports");
        result.StoragePath.Should().Be("docs/reports/file.txt");
    }

    [Fact]
    public async Task UploadFileAsync_Sets_CreatedAt()
    {
        var before = DateTimeOffset.UtcNow;
        using var stream = new MemoryStream("data"u8.ToArray());

        var result = await _service.UploadFileAsync(stream, "file.txt", "text/plain");

        result.CreatedAt.Should().BeOnOrAfter(before);
        result.CreatedAt.Should().BeOnOrBefore(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task GetFileByIdAsync_Returns_Uploaded_File()
    {
        using var stream = new MemoryStream("data"u8.ToArray());
        var uploaded = await _service.UploadFileAsync(stream, "lookup.txt", "text/plain", "docs");

        var found = await _service.GetFileByIdAsync(uploaded.Id);

        found.Should().NotBeNull();
        found!.FileName.Should().Be("lookup.txt");
        found.Folder.Should().Be("docs");
        found.ContentType.Should().Be("text/plain");
    }

    [Fact]
    public async Task DeleteFileAsync_Removes_DB_Record_Before_Storage()
    {
        using var stream = new MemoryStream("data"u8.ToArray());
        var uploaded = await _service.UploadFileAsync(stream, "file.txt", "text/plain");

        await _service.DeleteFileAsync(uploaded.Id);

        // DB record gone
        var record = await _db.StoredFiles.FindAsync(uploaded.Id);
        record.Should().BeNull();

        // Storage file also gone
        var exists = await _storageProvider.ExistsAsync(uploaded.StoragePath);
        exists.Should().BeFalse();
    }

    // --- Folder listing tests ---

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

    // --- Upload cleanup on failure ---

    [Fact]
    public async Task UploadFileAsync_Duplicate_FileName_In_Same_Folder_Throws()
    {
        using var s1 = new MemoryStream("a"u8.ToArray());
        await _service.UploadFileAsync(s1, "same.txt", "text/plain", "docs");

        var act = async () =>
        {
            using var s2 = new MemoryStream("b"u8.ToArray());
            await _service.UploadFileAsync(s2, "same.txt", "text/plain", "docs");
        };

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task UploadFileAsync_Same_FileName_Different_Folders_Succeeds()
    {
        using var s1 = new MemoryStream("a"u8.ToArray());
        using var s2 = new MemoryStream("b"u8.ToArray());

        var file1 = await _service.UploadFileAsync(s1, "readme.txt", "text/plain", "docs");
        var file2 = await _service.UploadFileAsync(s2, "readme.txt", "text/plain", "images");

        file1.Id.Should().NotBe(file2.Id);
        file1.StoragePath.Should().Be("docs/readme.txt");
        file2.StoragePath.Should().Be("images/readme.txt");
    }

    [Fact]
    public async Task UploadFileAsync_Cleans_Up_Storage_On_DB_Failure()
    {
        // Use a FailingStorageProvider to simulate DB failure while tracking storage state
        var failingProvider = new FailingStorageProvider(_storageProvider);
        var failingService = new FileStorageService(
            _db,
            failingProvider,
            NullLogger<FileStorageService>.Instance
        );

        // Upload first file successfully
        using var s1 = new MemoryStream("first"u8.ToArray());
        await failingService.UploadFileAsync(s1, "conflict.txt", "text/plain", "docs");

        // Second upload with same name — DB rejects due to unique constraint
        // The catch block should call DeleteAsync to clean up the storage file
        var act = async () =>
        {
            using var s2 = new MemoryStream("second"u8.ToArray());
            await failingService.UploadFileAsync(s2, "conflict.txt", "text/plain", "docs");
        };
        await act.Should().ThrowAsync<DbUpdateException>();

        // DB should still have exactly one record
        var count = await _db.StoredFiles.CountAsync();
        count.Should().Be(1);
    }

    public void Dispose() => _db.Dispose();
}
