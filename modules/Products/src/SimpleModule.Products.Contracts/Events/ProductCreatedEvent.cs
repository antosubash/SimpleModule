using SimpleModule.Core.Events;

namespace SimpleModule.Products.Contracts.Events;

public sealed record ProductCreatedEvent(ProductId ProductId, string Name, decimal Price) : IEvent;
