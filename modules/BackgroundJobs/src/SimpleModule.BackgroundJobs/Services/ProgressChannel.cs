using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace SimpleModule.BackgroundJobs.Services;

public sealed partial class ProgressChannel(ILogger<ProgressChannel>? logger = null)
{
    private readonly Channel<ProgressEntry> _channel = Channel.CreateUnbounded<ProgressEntry>(
        new UnboundedChannelOptions { SingleReader = true }
    );

    public ChannelReader<ProgressEntry> Reader => _channel.Reader;

    public void Enqueue(ProgressEntry entry)
    {
        if (!_channel.Writer.TryWrite(entry))
        {
            LogDropped(logger);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Progress entry dropped — channel closed")]
    private static partial void LogDropped(ILogger? logger);
}
