# SimpleModule.Blazor

[![NuGet](https://img.shields.io/nuget/v/SimpleModule.Blazor.svg)](https://www.nuget.org/packages/SimpleModule.Blazor)

Blazor SSR integration for SimpleModule, bridging server-side rendering with Inertia.js and React.

## Installation

```bash
dotnet add package SimpleModule.Blazor
```

## Quick Start

The Blazor package is automatically configured when using SimpleModule.Hosting. It provides the SSR shell that renders the initial HTML page with Inertia.js props for React hydration.

```csharp
// In an endpoint:
return Inertia.Render("Products/Browse", new { Products = products });
```

## Key Features

- **Blazor SSR shell** renders the initial HTML with embedded JSON props
- **Inertia.js middleware** bridges ASP.NET responses to React client navigation
- **Razor component discovery** for module-provided Blazor components

## Links

- [GitHub Repository](https://github.com/antosubash/SimpleModule)
