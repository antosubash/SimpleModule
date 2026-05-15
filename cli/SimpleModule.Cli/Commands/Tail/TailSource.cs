using System.Collections.Generic;

namespace SimpleModule.Cli.Commands.Tail;

/// <summary>
/// Abstract source of log lines. Implementations produce strings until the underlying
/// stream is exhausted or cancellation is requested.
/// </summary>
public abstract class TailSource
{
    /// <summary>
    /// A human-readable name for the source. Used as the file prefix when multiple
    /// sources are tailed concurrently.
    /// </summary>
    public abstract string Name { get; }

    public abstract IAsyncEnumerable<string> ReadLinesAsync(CancellationToken cancellationToken);
}
