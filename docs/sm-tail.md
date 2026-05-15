# sm tail

A Laravel Pail-equivalent log viewer for SimpleModule projects. `sm tail` reads
log lines from standard input or one or more files, pretty-prints them with
colour, and lets you slice the stream with simple filters.

## Overview

The SimpleModule framework does not ship a Serilog configuration. Most apps
write structured logs via `Microsoft.Extensions.Logging` to the console
(possibly with `AddJsonConsole`) or to a file. `sm tail` is designed around the
two formats you are most likely to encounter:

- **JSON-per-line** — Serilog compact JSON (`@t`, `@l`, `@m`, `@mt`, ...) or
  the built-in .NET `JsonConsoleFormatter` shape (`Timestamp`, `LogLevel`,
  `Category`, `Message`, `State`, ...).
- **Plain text** — best-effort regex extraction of timestamp, level, source,
  and message; lines that do not match the regex are still rendered, just with
  the entire line treated as the message.

Each input line is parsed independently. A line that starts with `{` is fed
to the JSON parser; anything else falls through to the plain-text parser.

## Usage

Pipe a running app into the viewer:

```bash
dotnet run --project template/SimpleModule.Host | sm tail
```

Tail one or more files (`--file` is repeatable):

```bash
sm tail --file logs/app.log
sm tail --file logs/app.log --file logs/jobs.log
```

When multiple files are passed, each output line is prefixed with `[filename]`
so you can tell streams apart.

One-shot read of an existing file (no follow):

```bash
sm tail --file logs/app.log --no-follow
```

Combine filters — level + substring + source:

```bash
sm tail --level Warning --filter checkout --source MyApp.Orders
```

Filter by structured properties (UserId, RequestId):

```bash
sm tail --user 42
sm tail --request 0HMVABCDEF
```

Pass-through raw JSON (useful for piping into `jq` or another tool):

```bash
sm tail --json --level Error | jq .
```

## Flag reference

| Flag             | Short | Description                                                                                                  |
| ---------------- | ----- | ------------------------------------------------------------------------------------------------------------ |
| `--level`        | `-l`  | Minimum level. Accepts `Trace`/`Debug`/`Information`/`Warning`/`Error`/`Critical` and short forms (`info`, `warn`, `err`). Case-insensitive. |
| `--filter`       | `-f`  | Substring match against the rendered message. Case-insensitive.                                              |
| `--source`       | `-s`  | Logger category. Equals match or namespace prefix (`Foo.Bar` matches `Foo.Bar.Baz`).                         |
| `--user`         | `-u`  | Match the `UserId` property (case-insensitive key lookup).                                                   |
| `--request`      | `-r`  | Match the `RequestId` property.                                                                              |
| `--json`         |       | Pass through raw JSON lines without coloring; skip plain-text lines. Useful for piping into other tools.     |
| `--no-follow`    |       | Read once and exit. Default is follow mode (tail forever, polling for appended bytes).                       |
| `--file <PATH>`  |       | Read from a file instead of stdin. Repeatable.                                                               |
| `--no-color`     |       | Disable colored output. Auto-disabled when `NO_COLOR` is set or stdout is redirected.                        |

## JSON vs plain-text auto-detection

Detection happens per line by checking whether the trimmed line starts with
`{`. This means a single file or stream can mix JSON and plain-text lines
freely — boot-time stack traces, Kestrel banners, and structured logs all
render correctly in the same session.

Recognised JSON keys (case-insensitive):

- **Timestamp**: `@t`, `Timestamp`, `timestamp`
- **Level**: `@l`, `Level`, `level`, `LogLevel`
- **Message**: `@m`, `Message`, `message` (falls back to `@mt` /
  `MessageTemplate` if no rendered message is present)
- **Source / category**: `SourceContext`, `Category`, `Logger`
- **State**: when present and a JSON object (the `JsonConsoleFormatter` shape),
  its keys are flattened into the properties dictionary; `Message` and
  `{OriginalFormat}` inside `State` populate the message / message template.
- **EventId**: when present as an object, exposed as `EventId.Id` /
  `EventId.Name` properties.

Anything else in the JSON object becomes a structured property visible to the
`--user`, `--request`, and (in JSON pass-through mode) the rendered tail.

## Output

Coloured rendering uses Spectre.Console:

- Timestamp — grey, local time, `HH:mm:ss.fff`
- Level — three-letter shorthand (`TRC`/`DBG`/`INF`/`WRN`/`ERR`/`CRT`/`FTL`),
  red for errors, yellow for warnings, cyan for information, grey for
  debug/trace.
- Source — italic grey
- Message — default colour
- Properties — dim, rendered as `{ key=value, ... }`

`--no-color`, the `NO_COLOR` environment variable, and a redirected stdout all
disable colouring (and skip the markup entirely so piped output is clean).

## Cancellation

`Ctrl+C` cancels follow mode and drains any lines already buffered in memory
before exiting. The exit code is `0`.

## Future work

- **Merged multi-file tail by timestamp.** When tailing multiple sources from
  the SimpleModule AppHost orchestrator (`sm dev`), it would be useful to
  merge streams by timestamp rather than printing as they arrive. This is not
  yet implemented; multiple `--file` flags currently produce interleaved
  output ordered by arrival time only. Each line is still prefixed with its
  filename so streams remain distinguishable.
- **Tail HTTP / SignalR streams.** A future addition could subscribe to a
  framework-supplied debug stream rather than scraping a local file.
