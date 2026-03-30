using SimpleModule.Core.Events;

namespace SimpleModule.Tenants.Contracts.Events;

public sealed record TenantStatusChangedEvent(
    TenantId TenantId,
    TenantStatus OldStatus,
    TenantStatus NewStatus
) : IEvent;
