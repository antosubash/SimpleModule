using System.Reflection;
using FluentAssertions;
using SimpleModule.Core.Broadcasting;

namespace SimpleModule.Core.Tests.Broadcasting;

public class BroadcastEventAttributeTests
{
    [BroadcastEvent("orders.created")]
    private sealed record OrderCreated(Guid Id) : IBroadcastEvent
    {
        public Guid EventId { get; } = Guid.CreateVersion7();
        public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

        public string Channel(IBroadcastContext context) => $"orders.{Id}";
    }

    [Fact]
    public void Attribute_Carries_Wire_Name()
    {
        var attr = typeof(OrderCreated).GetCustomAttribute<BroadcastEventAttribute>();

        attr.Should().NotBeNull();
        attr!.Name.Should().Be("orders.created");
    }

    [Fact]
    public void Channel_Uses_Event_State()
    {
        var id = Guid.NewGuid();
        var evt = new OrderCreated(id);

        evt.Channel(BroadcastContext.Empty).Should().Be($"orders.{id}");
    }
}
