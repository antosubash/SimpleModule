using SimpleModule.Notifications.Contracts;

namespace SimpleModule.Notifications.Channels;

public interface INotificationChannelRegistry
{
    INotificationChannel? Find(string name);
    IReadOnlyCollection<string> RegisteredNames { get; }
}

public sealed class NotificationChannelRegistry : INotificationChannelRegistry
{
    private readonly Dictionary<string, INotificationChannel> _channels;

    public NotificationChannelRegistry(IEnumerable<INotificationChannel> channels)
    {
        _channels = channels.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
    }

    public INotificationChannel? Find(string name) =>
        _channels.TryGetValue(name, out var channel) ? channel : null;

    public IReadOnlyCollection<string> RegisteredNames => _channels.Keys;
}
