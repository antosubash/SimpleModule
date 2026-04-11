using SimpleModule.Core.Events;

namespace SimpleModule.Products.Contracts.Events;

public sealed record ProductUpdatedEvent(ProductId ProductId, string Name, decimal Price) : IEvent;
