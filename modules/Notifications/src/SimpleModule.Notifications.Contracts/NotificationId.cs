using Vogen;

namespace SimpleModule.Notifications.Contracts;

[ValueObject<Guid>(conversions: Conversions.SystemTextJson | Conversions.EfCoreValueConverter)]
public readonly partial struct NotificationId;
