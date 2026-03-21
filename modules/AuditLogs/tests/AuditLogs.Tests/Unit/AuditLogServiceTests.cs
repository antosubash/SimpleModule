using AuditLogs.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SimpleModule.AuditLogs;
using SimpleModule.AuditLogs.Contracts;

namespace AuditLogs.Tests.Unit;

public sealed class AuditLogServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly AuditLogsDbContext _db;
    private readonly AuditLogService _sut;

    public AuditLogServiceTests()
    {
        _db = _factory.Create();
        _sut = new AuditLogService(_db, NullLogger<AuditLogService>.Instance);
    }

    public void Dispose()
    {
        _db.Dispose();
        _factory.Dispose();
    }

    private static AuditEntry CreateEntry(
        string? module = null,
        DateTimeOffset? timestamp = null,
        Guid? correlationId = null
    )
    {
        return new AuditEntry
        {
            Source = AuditSource.Http,
            Timestamp = timestamp ?? DateTimeOffset.UtcNow,
            CorrelationId = correlationId ?? Guid.NewGuid(),
            Module = module,
            Path = "/test",
            UserId = "user-1",
        };
    }

    [Fact]
    public async Task QueryAsync_WithDefaults_ReturnsResults()
    {
        _db.AuditEntries.AddRange(CreateEntry(), CreateEntry());
        await _db.SaveChangesAsync();

        // This is the exact scenario that caused the 500 error:
        // no query params → Page/PageSize/SortBy/SortDescending are null
        var result = await _sut.QueryAsync(new AuditQueryRequest());

        result.Items.Should().HaveCount(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(50);
    }

    [Fact]
    public async Task QueryAsync_ReturnsFilteredResults()
    {
        var entries = new List<AuditEntry>
        {
            CreateEntry(module: "Products"),
            CreateEntry(module: "Products"),
            CreateEntry(module: "Orders"),
        };
        _db.AuditEntries.AddRange(entries);
        await _db.SaveChangesAsync();

        var result = await _sut.QueryAsync(new AuditQueryRequest { Module = "Products" });

        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(e => e.Module.Should().Be("Products"));
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task QueryAsync_PaginatesCorrectly()
    {
        var entries = Enumerable
            .Range(0, 15)
            .Select(i =>
                CreateEntry(module: "Test", timestamp: DateTimeOffset.UtcNow.AddMinutes(-i))
            )
            .ToList();
        _db.AuditEntries.AddRange(entries);
        await _db.SaveChangesAsync();

        var result = await _sut.QueryAsync(
            new AuditQueryRequest
            {
                Page = 2,
                PageSize = 10,
                SortDescending = false,
            }
        );

        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(15);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsEntry()
    {
        var entry = CreateEntry(module: "Products");
        _db.AuditEntries.Add(entry);
        await _db.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(entry.Id);

        result.Should().NotBeNull();
        result!.Module.Should().Be("Products");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullForUnknownId()
    {
        var result = await _sut.GetByIdAsync(AuditEntryId.From(99999));

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByCorrelationIdAsync_ReturnsCorrelatedEntries()
    {
        var correlationId = Guid.NewGuid();
        var entries = new List<AuditEntry>
        {
            CreateEntry(module: "Products", correlationId: correlationId),
            CreateEntry(module: "Products", correlationId: correlationId),
            CreateEntry(module: "Orders"), // different correlation
        };
        _db.AuditEntries.AddRange(entries);
        await _db.SaveChangesAsync();

        var result = await _sut.GetByCorrelationIdAsync(correlationId);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(e => e.CorrelationId.Should().Be(correlationId));
    }

    [Fact]
    public async Task WriteBatchAsync_PersistsEntries()
    {
        var entries = new List<AuditEntry>
        {
            CreateEntry(module: "Products"),
            CreateEntry(module: "Orders"),
            CreateEntry(module: "Users"),
        };

        await _sut.WriteBatchAsync(entries);

        var result = await _sut.QueryAsync(new AuditQueryRequest { PageSize = 50 });
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task PurgeOlderThanAsync_DeletesOldEntries()
    {
        var oldEntry = CreateEntry(module: "Old", timestamp: DateTimeOffset.UtcNow.AddDays(-30));
        var newEntry = CreateEntry(module: "New", timestamp: DateTimeOffset.UtcNow);
        _db.AuditEntries.AddRange(oldEntry, newEntry);
        await _db.SaveChangesAsync();

        var cutoff = DateTimeOffset.UtcNow.AddDays(-7);
        var deletedCount = await _sut.PurgeOlderThanAsync(cutoff);

        deletedCount.Should().Be(1);
        var remaining = await _sut.QueryAsync(new AuditQueryRequest { PageSize = 50 });
        remaining.TotalCount.Should().Be(1);
        remaining.Items.Single().Module.Should().Be("New");
    }
}
