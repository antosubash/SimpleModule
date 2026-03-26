# SimpleModule.Generator

[![NuGet](https://img.shields.io/nuget/v/SimpleModule.Generator.svg)](https://www.nuget.org/packages/SimpleModule.Generator)

Roslyn source generator that auto-discovers modules, endpoints, and DTOs at compile time.

## Installation

```bash
dotnet add package SimpleModule.Generator
```

## Quick Start

The generator runs automatically at build time. Reference it in your host project and call the generated extension methods:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddModules(); // Generated method

var app = builder.Build();
app.MapModuleEndpoints(); // Generated method
```

## What It Generates

- **AddModules()** -- registers all discovered module services
- **MapModuleEndpoints()** -- maps all IEndpoint implementations
- **CollectModuleMenuItems()** -- gathers menu registrations from all modules
- **JSON serializer contexts** for all [Dto] types
- **TypeScript interface definitions** embedded as resources
- **Razor component assembly discovery** for Blazor SSR

## Key Features

- Compile-time module discovery with zero runtime reflection
- Incremental generation via Roslyn IIncrementalGenerator
- Targets netstandard2.0 for broad analyzer compatibility

## Links

- [GitHub Repository](https://github.com/antosubash/SimpleModule)
