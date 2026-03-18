using SimpleModule.Core.Events;
using SimpleModule.Core.Ids;

namespace SimpleModule.Orders.Contracts.Events;

public sealed record OrderCreatedEvent(OrderId OrderId, UserId UserId, decimal Total) : IEvent;
