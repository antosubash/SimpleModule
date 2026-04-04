# SimpleModule.BackgroundJobs

Background job management module for [SimpleModule](https://github.com/antosubash/SimpleModule) — a modular monolith framework for .NET.

## Features

- Scheduled background job execution using TickerQ
- Support for time-based and cron job scheduling
- Job progress tracking and status monitoring
- Admin dashboard for job management

## Installation

```bash
sm install SimpleModule.BackgroundJobs
```

Or via .NET CLI:

```bash
dotnet add package SimpleModule.BackgroundJobs
```

## Usage

The module is auto-discovered by the SimpleModule framework. Register your background jobs using the provided job scheduling APIs.

## License

MIT
