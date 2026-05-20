---
outline: deep
---

# Notifications

The Notifications module gives every module one way to reach a user — over mail, SMS, or the in-app inbox — without coupling to the delivery channel. You write a notification type once; the channels decide what to send.

## Defining a notification

Implement `INotification` and declare which channels should attempt delivery. Return a payload only for the channels you implement; channels you skip are quietly ignored.

```csharp
using SimpleModule.Notifications.Contracts;
using static SimpleModule.Notifications.Contracts.NotificationsConstants.Channels;

public sealed class OrderShipped(string trackingNumber) : INotification
{
    public string NotificationType => "orders.shipped";

    public string[] Via(NotificationRecipient recipient) => [Mail, Database];

    public MailMessage ToMail(NotificationRecipient recipient) => new(
        subject: "Your order is on its way",
        body: $"Track it: {trackingNumber}");

    public DatabaseNotificationPayload ToDatabase(NotificationRecipient recipient) => new(
        title: "Order shipped",
        body: trackingNumber,
        data: new { trackingNumber });
}
```

Channel constants live on `NotificationsConstants.Channels`: `Mail`, `Database`, `Sms`.

## Sending

Inject `INotifier` and call `SendAsync` for normal traffic, `SendNowAsync` for tests or flows where the caller wants delivery errors to surface immediately:

```csharp
public sealed class OrderService(INotifier notifier)
{
    public async Task ShipAsync(Order order, CancellationToken ct)
    {
        // ...
        var recipient = new NotificationRecipient(order.UserId, order.Email);
        await notifier.SendAsync(recipient, new OrderShipped(order.TrackingNumber), ct);
    }
}
```

`SendAsync` queues delivery via the BackgroundJobs module so the caller does not block on slow channels (SMTP, SMS gateway). `SendNowAsync` dispatches synchronously on the calling thread.

## Channels

Each channel is enabled per environment via Settings:

| Setting key | Default | Description |
|---|---|---|
| `notifications.channel.mail.enabled` | `true` | Toggles `MailChannel`, which calls into the Email module. |
| `notifications.channel.database.enabled` | `true` | Toggles persistence to the inbox table read by the `/notifications/` UI. |
| `notifications.channel.sms.enabled` | `false` | Toggles `SmsChannel`, which uses the registered `ISmsSender`. |
| `notifications.defaultPageSize` | `20` | Default page size for inbox queries. |

A channel only attempts delivery if (a) the notification's `Via(...)` includes its name and (b) the channel is enabled.

## In-app inbox

When the `Database` channel persists a notification it shows up in the recipient's inbox at `/notifications/`. Modules that need to read or mutate the inbox depend on the contract:

```csharp
public interface INotificationsContracts
{
    Task<PagedResult<Notification>> ListAsync(UserId userId, QueryNotificationsRequest request);
    Task<int> GetUnreadCountAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<Notification?> GetByIdAsync(NotificationId id, UserId userId);
    Task<bool> MarkReadAsync(NotificationId id, UserId userId);
    Task<int> MarkAllReadAsync(UserId userId);
}
```

Inbox API endpoints (`/api/notifications/`):

| Method | Route | Description |
|---|---|---|
| `GET` | `/` | List notifications for the current user (paged, optional `?unread=true`). |
| `GET` | `/unread-count` | Unread count — useful for badges. |
| `POST` | `/{id}/read` | Mark one notification as read. |
| `POST` | `/read-all` | Mark all unread notifications as read. |

## Domain events

Every send attempt emits exactly one of:

- `NotificationSentEvent(UserId, NotificationType, Channel)`
- `NotificationFailedEvent(UserId, NotificationType, Channel, Error)`

These flow through the same [event bus](/guide/events) as the rest of the framework, so other modules can react (audit log, retry queue, alerting) without depending on the Notifications module directly.

## Live updates

For real-time inbox refresh, hook `NotificationSentEvent` and republish through [Broadcasting](/guide/broadcasting):

```csharp
public sealed class NotificationBroadcastHandler(IBroadcaster broadcaster)
{
    public Task Handle(NotificationSentEvent evt, CancellationToken ct) =>
        broadcaster.ToUserAsync(
            evt.UserId.ToString(),
            "notifications.sent",
            new { evt.NotificationType, evt.Channel },
            ct);
}
```

The UI can subscribe with `useEvent(BroadcastChannels.ForUser(userId), 'notifications.sent', ...)` and refresh its inbox query when the event fires.

## Next Steps

- [Events](/guide/events) — durable event delivery the channels rely on.
- [Broadcasting](/guide/broadcasting) — push new inbox items to the browser in real time.
- [Background Jobs](/guide/background-jobs) — the queue behind `SendAsync`.
