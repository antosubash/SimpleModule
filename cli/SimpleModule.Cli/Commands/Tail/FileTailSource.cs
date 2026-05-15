using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SimpleModule.Cli.Commands.Tail;

public sealed class FileTailSource : TailSource
{
    private const int PollDelayMs = 250;

    private readonly string _path;
    private readonly bool _follow;

    public FileTailSource(string path, bool follow)
    {
        ArgumentNullException.ThrowIfNull(path);
        _path = path;
        _follow = follow;
    }

    public override string Name => Path.GetFileName(_path);

    public override async IAsyncEnumerable<string> ReadLinesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await using var stream = new FileStream(
            _path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete
        );

        if (_follow)
        {
            stream.Seek(0, SeekOrigin.End);
        }

        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true
        );

        var buffer = new StringBuilder();
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await ReadLineWithBufferAsync(reader, buffer, cancellationToken)
                .ConfigureAwait(false);

            if (line is not null)
            {
                yield return line;
                continue;
            }

            if (!_follow)
            {
                if (buffer.Length > 0)
                {
                    yield return buffer.ToString();
                }
                yield break;
            }

            try
            {
                await Task.Delay(PollDelayMs, cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                yield break;
            }
        }
    }

    private static async Task<string?> ReadLineWithBufferAsync(
        StreamReader reader,
        StringBuilder buffer,
        CancellationToken cancellationToken
    )
    {
        var charBuf = new char[1024];
        while (!cancellationToken.IsCancellationRequested)
        {
            // First, see if buffer already contains a complete line carried over from
            // a previous read. We can't just append-then-scan because the previous
            // carry-over may itself contain newlines.
            if (TryExtractLine(buffer, out var carried))
            {
                return carried;
            }

            var read = await reader
                .ReadAsync(charBuf.AsMemory(0, charBuf.Length), cancellationToken)
                .ConfigureAwait(false);
            if (read == 0)
            {
                return null;
            }

            buffer.Append(charBuf, 0, read);
        }
        return null;
    }

    private static bool TryExtractLine(StringBuilder buffer, out string line)
    {
        for (var i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] != '\n')
            {
                continue;
            }

            var end = i;
            if (end > 0 && buffer[end - 1] == '\r')
            {
                end--;
            }

            line = buffer.ToString(0, end);
            buffer.Remove(0, i + 1);
            return true;
        }

        line = string.Empty;
        return false;
    }
}
