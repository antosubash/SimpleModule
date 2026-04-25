using SimpleModule.Core.Events;

namespace SimpleModule.Items.Contracts.Events;

public sealed record ItemCreatedEvent(int ItemId) : IEvent;
