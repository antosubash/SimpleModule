# SimpleModule.Hosting

[![NuGet](https://img.shields.io/nuget/v/SimpleModule.Hosting.svg)](https://www.nuget.org/packages/SimpleModule.Hosting)

Host builder extensions for SimpleModule that configure module registration, endpoint mapping, and the middleware pipeline.

## Installation

```bash
dotnet add package SimpleModule.Hosting
```

## Quick Start

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddSimpleModule();

var app = builder.Build();
app.UseSimpleModule();
app.Run();
```

## Key Features

- **AddSimpleModule()** registers all framework services, modules, and database providers
- **UseSimpleModule()** configures middleware, maps endpoints, and sets up Inertia.js
- **Swagger/OpenAPI** integration via Swashbuckle
- **MSBuild targets** for build-time module wiring

## Links

- [GitHub Repository](https://github.com/antosubash/SimpleModule)
