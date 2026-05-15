using System.Threading.Channels;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.Tail;

public sealed class TailCommand : AsyncCommand<TailSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, TailSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var useColor = ResolveUseColor(settings);
        var console = CreateConsole(useColor);

        var sources = BuildSources(settings);
        if (sources.Count == 0)
        {
            console.MarkupLine(
                "[red]No log source available — pipe data into stdin or pass --file <path>.[/]"
            );
            return 1;
        }

        var includePrefix = sources.Count > 1;

        using var cts = new CancellationTokenSource();
        ConsoleCancelEventHandler cancelHandler = (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };
        Console.CancelKeyPress += cancelHandler;

        try
        {
            var channel = Channel.CreateUnbounded<(string Line, string Source)>(
                new UnboundedChannelOptions { SingleReader = true, SingleWriter = false }
            );

            var producers = sources
                .Select(source => RunProducerAsync(source, channel.Writer, cts.Token))
                .ToList();

            var completion = Task.WhenAll(producers)
                .ContinueWith(_ => channel.Writer.TryComplete(), TaskScheduler.Default);

            await ConsumeAsync(
                    channel.Reader,
                    console,
                    settings,
                    useColor,
                    includePrefix,
                    cts.Token
                )
                .ConfigureAwait(false);

            await completion.ConfigureAwait(false);

            return cts.IsCancellationRequested ? 0 : 0;
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }
    }

    private static List<TailSource> BuildSources(TailSettings settings)
    {
        var sources = new List<TailSource>();
        if (settings.Files is { Length: > 0 } files)
        {
            foreach (var file in files)
            {
                if (string.IsNullOrWhiteSpace(file))
                {
                    continue;
                }

                if (!File.Exists(file))
                {
                    AnsiConsole.MarkupLine(
                        $"[yellow]Skipping missing file:[/] {Markup.Escape(file)}"
                    );
                    continue;
                }

                sources.Add(new FileTailSource(file, follow: !settings.NoFollow));
            }
        }
        else
        {
            sources.Add(new StdinTailSource());
        }

        return sources;
    }

    private static async Task RunProducerAsync(
        TailSource source,
        ChannelWriter<(string Line, string Source)> writer,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await foreach (
                var line in source.ReadLinesAsync(cancellationToken).ConfigureAwait(false)
            )
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                await writer
                    .WriteAsync((line, source.Name), cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on Ctrl+C
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
#pragma warning restore CA1031
        {
            AnsiConsole.MarkupLine(
                $"[red][[{Markup.Escape(source.Name)}]][/] {Markup.Escape(ex.Message)}"
            );
        }
    }

    private static async Task ConsumeAsync(
        ChannelReader<(string Line, string Source)> reader,
        IAnsiConsole console,
        TailSettings settings,
        bool useColor,
        bool includePrefix,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await foreach (
                var (line, sourceName) in reader
                    .ReadAllAsync(cancellationToken)
                    .ConfigureAwait(false)
            )
            {
                ProcessLine(console, settings, useColor, includePrefix, line, sourceName);
            }
        }
        catch (OperationCanceledException)
        {
            // Drain whatever is already in the channel so users see the last few lines
            while (reader.TryRead(out var item))
            {
                ProcessLine(console, settings, useColor, includePrefix, item.Line, item.Source);
            }
        }
    }

    private static void ProcessLine(
        IAnsiConsole console,
        TailSettings settings,
        bool useColor,
        bool includePrefix,
        string line,
        string sourceName
    )
    {
        if (settings.Json)
        {
            // Raw passthrough; skip non-JSON lines.
            var trimmed = line.TrimStart();
            if (!trimmed.StartsWith('{'))
            {
                return;
            }

            if (!LogEntryParser.TryParseJson(line, out var jsonEntry))
            {
                return;
            }

            if (!LogEntryFilter.Matches(jsonEntry, settings))
            {
                return;
            }

            var prefix = includePrefix ? "[" + sourceName + "] " : string.Empty;
            console.WriteLine(prefix + line);
            return;
        }

        var entry = LogEntryParser.Parse(line);
        if (!LogEntryFilter.Matches(entry, settings))
        {
            return;
        }

        LogEntryRenderer.Render(console, entry, useColor, includePrefix ? sourceName : null);
    }

    private static bool ResolveUseColor(TailSettings settings)
    {
        if (settings.NoColor)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR")))
        {
            return false;
        }

        try
        {
            if (Console.IsOutputRedirected)
            {
                return false;
            }
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch
#pragma warning restore CA1031
        {
            // Treat probing failures as "not a tty"
            return false;
        }

        return true;
    }

    private static IAnsiConsole CreateConsole(bool useColor)
    {
        if (useColor)
        {
            return AnsiConsole.Console;
        }

        return AnsiConsole.Create(
            new AnsiConsoleSettings
            {
                Ansi = AnsiSupport.No,
                ColorSystem = ColorSystemSupport.NoColors,
                Out = new AnsiConsoleOutput(Console.Out),
            }
        );
    }
}
