using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.Database;
using SimpleModule.Datasets.Contracts;
using SimpleModule.Datasets.Entities;
using SimpleModule.Datasets.Jobs;
using SimpleModule.Storage;

namespace SimpleModule.Datasets.Tests;

public sealed class PurgeDatasetJobTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public PurgeDatasetJobTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task ExecuteAsync_Deletes_Original_Normalized_And_All_Derivatives()
    {
        await using var db = CreateDbContext();
        var storage = new InMemoryStorage();

        var id = DatasetId.From(Guid.NewGuid());
        const string original = "datasets/abc/original.geojson";
        const string normalized = "datasets/abc/normalized.geojson";
        const string derivativePmTiles = "datasets/abc/derivatives/PmTiles.pmtiles";
        const string derivativeCog = "datasets/abc/derivatives/Cog.tif";

        storage.Seed(original);
        storage.Seed(normalized);
        storage.Seed(derivativePmTiles);
        storage.Seed(derivativeCog);

        var metadata = new DatasetMetadata
        {
            Derivatives =
            {
                new DatasetDerivative
                {
                    Format = DatasetFormat.PmTiles,
                    StoragePath = derivativePmTiles,
                    SizeBytes = 1024,
                    CreatedAt = DateTimeOffset.UtcNow,
                },
                new DatasetDerivative
                {
                    Format = DatasetFormat.Cog,
                    StoragePath = derivativeCog,
                    SizeBytes = 2048,
                    CreatedAt = DateTimeOffset.UtcNow,
                },
            },
        };

        db.Datasets.Add(
            new Dataset
            {
                Id = id,
                Name = "sample",
                OriginalFileName = "sample.geojson",
                Format = DatasetFormat.GeoJson,
                Status = DatasetStatus.Ready,
                StoragePath = original,
                NormalizedPath = normalized,
                SizeBytes = 100,
                MetadataJson = JsonSerializer.Serialize(metadata),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                IsDeleted = true,
                DeletedAt = DateTimeOffset.UtcNow,
                ConcurrencyStamp = Guid.NewGuid().ToString("N"),
            }
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var job = new PurgeDatasetJob(db, storage, NullLogger<PurgeDatasetJob>.Instance);
        var context = new FakeJobExecutionContext(
            JsonSerializer.Serialize(new PurgeDatasetJobData { DatasetId = id.Value })
        );

        await job.ExecuteAsync(context, TestContext.Current.CancellationToken);

        storage.Contains(original).Should().BeFalse();
        storage.Contains(normalized).Should().BeFalse();
        storage.Contains(derivativePmTiles).Should().BeFalse();
        storage.Contains(derivativeCog).Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_Finds_SoftDeleted_Row_Despite_QueryFilter()
    {
        await using var db = CreateDbContext();
        var storage = new InMemoryStorage();

        var id = DatasetId.From(Guid.NewGuid());
        const string original = "datasets/xyz/original.geojson";
        storage.Seed(original);

        db.Datasets.Add(
            new Dataset
            {
                Id = id,
                Name = "hidden",
                OriginalFileName = "hidden.geojson",
                Format = DatasetFormat.GeoJson,
                Status = DatasetStatus.Ready,
                StoragePath = original,
                SizeBytes = 10,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                IsDeleted = true,
                DeletedAt = DateTimeOffset.UtcNow,
                ConcurrencyStamp = Guid.NewGuid().ToString("N"),
            }
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Sanity check: default query filter hides the row.
        (
            await db.Datasets.FirstOrDefaultAsync(
                d => d.Id == id,
                TestContext.Current.CancellationToken
            )
        )
            .Should()
            .BeNull();

        var job = new PurgeDatasetJob(db, storage, NullLogger<PurgeDatasetJob>.Instance);
        var context = new FakeJobExecutionContext(
            JsonSerializer.Serialize(new PurgeDatasetJobData { DatasetId = id.Value })
        );

        await job.ExecuteAsync(context, TestContext.Current.CancellationToken);

        storage.Contains(original).Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_Missing_Dataset_Returns_Without_Throwing()
    {
        await using var db = CreateDbContext();
        var storage = new InMemoryStorage();

        var job = new PurgeDatasetJob(db, storage, NullLogger<PurgeDatasetJob>.Instance);
        var context = new FakeJobExecutionContext(
            JsonSerializer.Serialize(new PurgeDatasetJobData { DatasetId = Guid.NewGuid() })
        );

        var act = async () =>
            await job.ExecuteAsync(context, TestContext.Current.CancellationToken);
        await act.Should().NotThrowAsync();
    }

    private TestDatasetsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DatasetsDbContext>()
            .UseSqlite(_connection)
            .Options;
        var dbOptions = Options.Create(
            new DatabaseOptions { DefaultConnection = "Data Source=:memory:" }
        );
        var context = new TestDatasetsDbContext(options, dbOptions);
        context.Database.EnsureCreated();
        return context;
    }

    private sealed class TestDatasetsDbContext(
        DbContextOptions<DatasetsDbContext> options,
        IOptions<DatabaseOptions> dbOptions
    ) : DatasetsDbContext(options, dbOptions)
    {
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);
            configurationBuilder
                .Properties<DateTimeOffset>()
                .HaveConversion<DateTimeOffsetToStringConverter>();
            configurationBuilder
                .Properties<DateTimeOffset?>()
                .HaveConversion<DateTimeOffsetToStringConverter>();
        }
    }

    private sealed class InMemoryStorage : IStorageProvider
    {
        private readonly HashSet<string> _paths = new(StringComparer.Ordinal);

        public void Seed(string path) => _paths.Add(path);

        public bool Contains(string path) => _paths.Contains(path);

        public Task<StorageResult> SaveAsync(
            string path,
            Stream content,
            string contentType,
            CancellationToken cancellationToken = default
        )
        {
            _paths.Add(path);
            return Task.FromResult(new StorageResult(path, 0, contentType));
        }

        public Task<Stream?> GetAsync(string path, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream?>(_paths.Contains(path) ? new MemoryStream() : null);

        public Task<bool> DeleteAsync(string path, CancellationToken cancellationToken = default) =>
            Task.FromResult(_paths.Remove(path));

        public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default) =>
            Task.FromResult(_paths.Contains(path));

        public Task<IReadOnlyList<StorageEntry>> ListAsync(
            string prefix,
            CancellationToken cancellationToken = default
        ) => Task.FromResult<IReadOnlyList<StorageEntry>>([]);
    }

    private sealed class FakeJobExecutionContext(string data) : IJobExecutionContext
    {
        public JobId JobId { get; } = JobId.From(Guid.NewGuid());

        public T GetData<T>() =>
            JsonSerializer.Deserialize<T>(data)
            ?? throw new InvalidOperationException("null payload");

        public void ReportProgress(int percentage, string? message = null) { }

        public void Log(string message) { }
    }
}
