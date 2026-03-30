using SimpleModule.Core.Events;

namespace SimpleModule.Tenants.Contracts.Events;

public sealed record TenantHostAddedEvent(TenantId TenantId, string HostName) : IEvent;
