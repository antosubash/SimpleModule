# SimpleModule.RateLimiting

Rate limiting module for [SimpleModule](https://github.com/antosubash/SimpleModule) — a modular monolith framework for .NET.

## Features

- Fixed window, sliding window, and token bucket algorithms
- IP-based and user-based rate limiting
- Configurable policies per endpoint
- Middleware integration for automatic enforcement

## Installation

```bash
sm install SimpleModule.RateLimiting
```

Or via .NET CLI:

```bash
dotnet add package SimpleModule.RateLimiting
```

## Usage

The module is auto-discovered by the SimpleModule framework. Configure rate limiting policies via module options.

## License

MIT
