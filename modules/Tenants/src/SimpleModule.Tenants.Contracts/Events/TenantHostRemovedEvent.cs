using SimpleModule.Core.Events;

namespace SimpleModule.Tenants.Contracts.Events;

public sealed record TenantHostRemovedEvent(TenantId TenantId, string HostName) : IEvent;
