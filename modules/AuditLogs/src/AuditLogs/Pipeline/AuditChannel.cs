using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using SimpleModule.AuditLogs.Contracts;

namespace SimpleModule.AuditLogs.Pipeline;

public sealed partial class AuditChannel(ILogger<AuditChannel>? logger = null)
{
    private readonly Channel<AuditEntry> _channel = Channel.CreateUnbounded<AuditEntry>(
        new UnboundedChannelOptions { SingleReader = true }
    );

    public ChannelReader<AuditEntry> Reader => _channel.Reader;

    public void Enqueue(AuditEntry entry)
    {
        if (!_channel.Writer.TryWrite(entry))
        {
            logger?.Log(LogLevel.Warning, "Audit entry dropped — channel closed or full");
        }
    }
}
