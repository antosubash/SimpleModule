# SimpleModule.Core

[![NuGet](https://img.shields.io/nuget/v/SimpleModule.Core.svg)](https://www.nuget.org/packages/SimpleModule.Core)

Core interfaces and attributes for the SimpleModule modular monolith framework.

## Installation

```bash
dotnet add package SimpleModule.Core
```

## Quick Start

```csharp
[Module("Products", RoutePrefix = "products")]
public sealed class ProductsModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Register module services
    }
}
```

## Key Features

- **IModule** interface for defining self-contained modules
- **IEndpoint** interface for auto-discovered Minimal API endpoints
- **[Dto]** attribute for cross-module data transfer types with TypeScript generation
- **IEventBus** for decoupled module-to-module communication
- **IMenuRegistry** for dynamic navigation menu registration
- **Inertia.js integration** for server-driven React page rendering

## Links

- [GitHub Repository](https://github.com/antosubash/SimpleModule)
