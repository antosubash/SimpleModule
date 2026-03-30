using SimpleModule.Core.Events;

namespace SimpleModule.Tenants.Contracts.Events;

public sealed record TenantUpdatedEvent(TenantId TenantId, string Name) : IEvent;
