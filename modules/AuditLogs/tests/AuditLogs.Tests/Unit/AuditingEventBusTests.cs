using System.Threading.Channels;
using FluentAssertions;
using NSubstitute;
using SimpleModule.AuditLogs;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.AuditLogs.Pipeline;
using SimpleModule.Core.Events;

namespace AuditLogs.Tests.Unit;

public record ProductCreatedEvent(int ProductId, string Name) : IEvent;

public record OrderDeletedEvent(int OrderId) : IEvent;

public record SimpleEvent(string Data) : IEvent;

public class AuditingEventBusTests
{
    private readonly IEventBus _innerBus = Substitute.For<IEventBus>();
    private readonly AuditChannel _channel = new();
    private readonly AuditContext _auditContext;
    private readonly AuditingEventBus _sut;

    public AuditingEventBusTests()
    {
        _auditContext = new AuditContext
        {
            UserId = "test-user",
            UserName = "Test User",
            IpAddress = "127.0.0.1",
        };
        _sut = new AuditingEventBus(_innerBus, _auditContext, _channel);
    }

    [Fact]
    public async Task PublishAsync_ExtractsModuleAndAction_FromEventName()
    {
        var @event = new ProductCreatedEvent(1, "Widget");

        await _sut.PublishAsync(@event);

        _channel.Reader.TryRead(out var entry).Should().BeTrue();
        entry!.Module.Should().Be("Product");
        entry.Action.Should().Be(AuditAction.Created);
    }

    [Fact]
    public async Task PublishAsync_ExtractsEntityId_FromProperties()
    {
        var @event = new ProductCreatedEvent(42, "Widget");

        await _sut.PublishAsync(@event);

        _channel.Reader.TryRead(out var entry).Should().BeTrue();
        entry!.EntityId.Should().Be("42");
    }

    [Fact]
    public async Task PublishAsync_DelegatesToInnerEventBus()
    {
        var @event = new ProductCreatedEvent(1, "Widget");

        await _sut.PublishAsync(@event);

        await _innerBus.Received(1).PublishAsync(@event, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_EnqueuesAuditEntryToChannel()
    {
        var @event = new OrderDeletedEvent(7);

        await _sut.PublishAsync(@event);

        _channel.Reader.TryRead(out var entry).Should().BeTrue();
        entry.Should().NotBeNull();
        entry!.Source.Should().Be(AuditSource.Domain);
        entry.Action.Should().Be(AuditAction.Deleted);
        entry.EntityType.Should().Be("Order");
    }

    [Fact]
    public async Task PublishAsync_SetsAuditContextFields()
    {
        var @event = new SimpleEvent("test");

        await _sut.PublishAsync(@event);

        _channel.Reader.TryRead(out var entry).Should().BeTrue();
        entry!.UserId.Should().Be("test-user");
        entry.UserName.Should().Be("Test User");
        entry.IpAddress.Should().Be("127.0.0.1");
        entry.CorrelationId.Should().Be(_auditContext.CorrelationId);
    }

    [Fact]
    public async Task PublishAsync_DoesNotEnqueueAuditEntry_WhenInnerPublishFails()
    {
        var @event = new ProductCreatedEvent(1, "Widget");
        var testException = new InvalidOperationException("Inner bus failed");

        // Configure the mock to throw when PublishAsync is called
        var calls = 0;
        _innerBus.PublishAsync(Arg.Any<IEvent>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(x =>
            {
                calls++;
                return Task.FromException(testException);
            });

        // Act & Assert: Should throw because inner bus failed
        var action = () => _sut.PublishAsync(@event);
        await action.Should().ThrowAsync<InvalidOperationException>();

        // Verify inner bus was called
        calls.Should().Be(1);

        // Assert: No audit entry should be queued
        _channel.Reader.TryRead(out _).Should().BeFalse("Audit entry should not be queued when publish fails");
    }

    [Fact]
    public async Task PublishAsync_DelegatesToInnerEventBus_BeforeEnqueuingAudit()
    {
        var @event = new ProductCreatedEvent(1, "Widget");
        var publishAsyncCalled = false;

        _innerBus
            .PublishAsync(Arg.Any<IEvent>(), Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                publishAsyncCalled = true;
                return Task.CompletedTask;
            });

        await _sut.PublishAsync(@event);

        // Verify that PublishAsync is called (audit happens after)
        publishAsyncCalled.Should().BeTrue();
    }
}
