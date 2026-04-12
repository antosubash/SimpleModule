using SimpleModule.Core.Events;

namespace SimpleModule.Products.Contracts.Events;

public sealed record ProductDeletedEvent(ProductId ProductId) : IEvent;
