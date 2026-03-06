using SimpleModule.Core.Events;

namespace SimpleModule.Orders.Contracts.Events;

public sealed record OrderCreatedEvent(int OrderId, int UserId, decimal Total) : IEvent;
