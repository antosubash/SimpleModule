using SimpleModule.Core.Events;

namespace SimpleModule.Tenants.Contracts.Events;

public sealed record TenantCreatedEvent(TenantId TenantId, string Name, string Slug) : IEvent;
