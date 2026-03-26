using FluentAssertions;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.AuditLogs.Pipeline;

namespace AuditLogs.Tests.Unit;

public class AuditChannelTests
{
    [Fact]
    public void Enqueue_SuccessfullyQueuesEntry()
    {
        // Arrange
        var channel = new AuditChannel();
        var entry = new AuditEntry
        {
            CorrelationId = Guid.NewGuid(),
            Source = AuditSource.Http,
            Timestamp = DateTimeOffset.UtcNow,
            Module = "Test",
            Path = "/api/test",
            UserId = "user-1",
        };

        // Act
        channel.Enqueue(entry);

        // Assert
        channel.Reader.TryRead(out var readEntry).Should().BeTrue();
        readEntry.Should().Be(entry);
    }

    [Fact]
    public void Enqueue_DoesNotThrow_WithValidEntry()
    {
        // Arrange
        var channel = new AuditChannel();
        var entry = new AuditEntry
        {
            CorrelationId = Guid.NewGuid(),
            Source = AuditSource.Http,
            Timestamp = DateTimeOffset.UtcNow,
            Module = "Test",
            Path = "/api/test",
            UserId = "user-1",
        };

        // Act & Assert
        var action = () => channel.Enqueue(entry);
        action.Should().NotThrow();
    }

    [Fact]
    public void Enqueue_CanQueueMultipleEntries()
    {
        // Arrange
        var channel = new AuditChannel();
        var entries = new[] { new AuditEntry(), new AuditEntry(), new AuditEntry() };

        // Act
        foreach (var entry in entries)
        {
            channel.Enqueue(entry);
        }

        // Assert - All entries should be queued
        var count = 0;
        while (channel.Reader.TryRead(out _))
        {
            count++;
        }
        count.Should().Be(3);
    }

    [Fact]
    public void Enqueue_HandlesMultipleEntries()
    {
        // Arrange
        var channel = new AuditChannel();
        var entries = new List<AuditEntry>
        {
            new()
            {
                CorrelationId = Guid.NewGuid(),
                Source = AuditSource.Http,
                Timestamp = DateTimeOffset.UtcNow,
                Module = "Test1",
                Path = "/api/test1",
                UserId = "user-1",
            },
            new()
            {
                CorrelationId = Guid.NewGuid(),
                Source = AuditSource.Domain,
                Timestamp = DateTimeOffset.UtcNow,
                Module = "Test2",
                Path = "/api/test2",
                UserId = "user-2",
            },
        };

        // Act
        foreach (var entry in entries)
        {
            channel.Enqueue(entry);
        }

        // Assert
        channel.Reader.TryRead(out var first).Should().BeTrue();
        first.Should().Be(entries[0]);

        channel.Reader.TryRead(out var second).Should().BeTrue();
        second.Should().Be(entries[1]);
    }

    [Fact]
    public void Reader_Property_IsAccessible()
    {
        // Arrange
        var channel = new AuditChannel();

        // Act
        var reader = channel.Reader;

        // Assert
        reader.Should().NotBeNull();
    }
}
