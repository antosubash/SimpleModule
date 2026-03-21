using System.Threading.Channels;
using SimpleModule.AuditLogs.Contracts;

namespace SimpleModule.AuditLogs.Pipeline;

public sealed class AuditChannel
{
    private readonly Channel<AuditEntry> _channel = Channel.CreateUnbounded<AuditEntry>(
        new UnboundedChannelOptions { SingleReader = true }
    );

    public ChannelReader<AuditEntry> Reader => _channel.Reader;

    public void Enqueue(AuditEntry entry)
    {
        _channel.Writer.TryWrite(entry);
    }
}
