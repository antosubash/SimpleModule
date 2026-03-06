using SimpleModule.Core.Events;

namespace SimpleModule.Orders.Contracts.Events;

public sealed record OrderCreatedEvent(int OrderId, string UserId, decimal Total) : IEvent;
