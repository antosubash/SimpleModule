# SimpleModule.Email

Email sending module for [SimpleModule](https://github.com/antosubash/SimpleModule) — a modular monolith framework for .NET.

## Features

- Configurable SMTP and log-based email providers
- Email template management
- Retry logic for failed deliveries
- Background job integration for async email sending
- Email history and tracking

## Installation

```bash
sm install SimpleModule.Email
```

Or via .NET CLI:

```bash
dotnet add package SimpleModule.Email
```

## Usage

The module is auto-discovered by the SimpleModule framework. Configure your SMTP settings in module options and use `IEmailContracts` to send emails from other modules.

## License

MIT
