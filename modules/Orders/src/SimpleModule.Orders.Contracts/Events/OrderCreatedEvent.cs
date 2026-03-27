using SimpleModule.Core.Events;

namespace SimpleModule.Orders.Contracts.Events;

public sealed record OrderCreatedEvent(OrderId OrderId, string UserId, decimal Total) : IEvent;
