# SimpleModule.OpenIddict

OAuth 2.0 / OpenID Connect authentication module for [SimpleModule](https://github.com/antosubash/SimpleModule) — a modular monolith framework for .NET.

## Features

- Full OAuth 2.0 and OpenID Connect server implementation via OpenIddict
- Client application management
- Token endpoints (authorization code, refresh token)
- Configurable scopes and claims
- Integration with ASP.NET Core Identity

## Installation

```bash
sm install SimpleModule.OpenIddict
```

Or via .NET CLI:

```bash
dotnet add package SimpleModule.OpenIddict
```

## Usage

The module is auto-discovered by the SimpleModule framework. Configure OAuth clients and scopes via module options.

## License

MIT
