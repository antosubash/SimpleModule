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

        var result = await _service.UploadFileAsync(stream, "photo.jpg", "image/jpeg", "products/images");

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

    public void Dispose() => _db.Dispose();
}
