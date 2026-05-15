using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SimpleModule.Cli.Commands.Tail;

public sealed class StdinTailSource : TailSource
{
    public override string Name => "stdin";

    public override async IAsyncEnumerable<string> ReadLinesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var reader = Console.In;
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                yield break;
            }
            yield return line;
        }
    }
}
