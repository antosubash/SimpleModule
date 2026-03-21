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
}
