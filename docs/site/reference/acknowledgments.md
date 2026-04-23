# Acknowledgments & Credits

SimpleModule is built on the shoulders of many excellent open-source projects. We are grateful to the maintainers and contributors of these libraries.

## .NET Packages

### Core Framework

| Package | Description | License |
|---------|-------------|---------|
| [.NET / ASP.NET Core](https://dotnet.microsoft.com/) | The runtime and web framework that powers SimpleModule | MIT |
| [Entity Framework Core](https://github.com/dotnet/efcore) | Modern object-database mapper for .NET | MIT |
| [Microsoft.AspNetCore.Identity.EntityFrameworkCore](https://github.com/dotnet/aspnetcore) | Identity management with EF Core integration | MIT |
| [Microsoft.AspNetCore.OpenApi](https://github.com/dotnet/aspnetcore) | OpenAPI document generation for ASP.NET Core | MIT |

### .NET Aspire

| Package | Description | License |
|---------|-------------|---------|
| [Aspire.Hosting.PostgreSQL](https://github.com/dotnet/aspire) | .NET Aspire PostgreSQL hosting integration | MIT |
| [Microsoft.Extensions.Http.Resilience](https://github.com/dotnet/extensions) | HTTP resilience and transient-fault handling | MIT |
| [Microsoft.Extensions.ServiceDiscovery](https://github.com/dotnet/extensions) | Service discovery for distributed applications | MIT |

### Database Providers

| Package | Description | License |
|---------|-------------|---------|
| [Npgsql.EntityFrameworkCore.PostgreSQL](https://github.com/npgsql/efcore.pg) | PostgreSQL provider for Entity Framework Core | PostgreSQL |
| [Microsoft.EntityFrameworkCore.Sqlite](https://github.com/dotnet/efcore) | SQLite provider for Entity Framework Core | MIT |
| [Microsoft.EntityFrameworkCore.SqlServer](https://github.com/dotnet/efcore) | SQL Server provider for Entity Framework Core | MIT |

### Authentication & Authorization

| Package | Description | License |
|---------|-------------|---------|
| [OpenIddict](https://github.com/openiddict/openiddict-core) | Versatile OpenID Connect stack for ASP.NET Core | Apache-2.0 |

### Observability (OpenTelemetry)

| Package | Description | License |
|---------|-------------|---------|
| [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet) | Distributed tracing, metrics, and logging for .NET | Apache-2.0 |

### API Documentation

| Package | Description | License |
|---------|-------------|---------|
| [Swashbuckle.AspNetCore](https://github.com/domaindrivendev/Swashbuckle.AspNetCore) | Swagger/OpenAPI tooling for ASP.NET Core APIs | MIT |

### Code Quality & Source Generators

| Package | Description | License |
|---------|-------------|---------|
| [Microsoft.CodeAnalysis.CSharp (Roslyn)](https://github.com/dotnet/roslyn) | The .NET compiler platform used for source generation | MIT |
| [Roslynator](https://github.com/dotnet/roslynator) | Extended collection of Roslyn analyzers and refactorings | Apache-2.0 |
| [NetEscapades.EnumGenerators](https://github.com/andrewlock/NetEscapades.EnumGenerators) | Source generator for fast enum helper methods | MIT |
| [Vogen](https://github.com/SteveDunn/Vogen) | Source generator for strongly-typed value objects | Apache-2.0 |

### Messaging & Background Jobs

| Package | Description | License |
|---------|-------------|---------|
| [Wolverine](https://github.com/JasperFx/wolverine) | In-process and distributed messaging used for the domain event bus and background job transport | MIT |
| [Cronos](https://github.com/HangfireIO/Cronos) | CRON expression parser for scheduled background jobs | MIT |

### AI Providers

| Package | Description | License |
|---------|-------------|---------|
| [Anthropic.SDK](https://github.com/tghamm/Anthropic.SDK) | .NET SDK for the Anthropic Claude API, used by `SimpleModule.AI.Anthropic` | MIT |

### Cloud Storage

| Package | Description | License |
|---------|-------------|---------|
| [Azure.Storage.Blobs](https://github.com/Azure/azure-sdk-for-net) | Azure Blob Storage client library | MIT |
| [AWSSDK.S3](https://github.com/aws/aws-sdk-net) | Amazon S3 client library for .NET | Apache-2.0 |

### CLI

| Package | Description | License |
|---------|-------------|---------|
| [Spectre.Console](https://github.com/spectreconsole/spectre.console) | Beautiful console applications with rich output | MIT |

### Testing

| Package | Description | License |
|---------|-------------|---------|
| [xUnit.v3](https://github.com/xunit/xunit) | Unit testing framework for .NET | Apache-2.0 |
| [FluentAssertions](https://github.com/fluentassertions/fluentassertions) | Fluent API for asserting results of unit tests | Apache-2.0 |
| [NSubstitute](https://github.com/nsubstitute/NSubstitute) | Friendly substitute for .NET mocking libraries | BSD-3-Clause |
| [Bogus](https://github.com/bchavez/Bogus) | Fake data generator for .NET | MIT |
| [Microsoft.AspNetCore.Mvc.Testing](https://github.com/dotnet/aspnetcore) | Integration testing infrastructure for ASP.NET Core | MIT |

## Frontend (npm) Packages

### Core Framework

| Package | Description | License |
|---------|-------------|---------|
| [React](https://github.com/facebook/react) | Library for building user interfaces | MIT |
| [Inertia.js](https://github.com/inertiajs/inertia) | Server-driven SPA framework | MIT |
| [TypeScript](https://github.com/microsoft/TypeScript) | Typed superset of JavaScript | Apache-2.0 |

### Build Tools

| Package | Description | License |
|---------|-------------|---------|
| [Vite](https://github.com/vitejs/vite) | Next-generation frontend build tool | MIT |
| [Tailwind CSS](https://github.com/tailwindlabs/tailwindcss) | Utility-first CSS framework | MIT |
| [VitePress](https://github.com/vuejs/vitepress) | Static site generator for documentation | MIT |

### UI Components (Radix UI)

| Package | Description | License |
|---------|-------------|---------|
| [Radix UI](https://github.com/radix-ui/primitives) | Unstyled, accessible UI component primitives for React | MIT |

SimpleModule uses the following Radix UI primitives: Accordion, Aspect Ratio, Avatar, Checkbox, Collapsible, Dialog, Dropdown Menu, Hover Card, Label, Popover, Progress, Radio Group, Scroll Area, Select, Separator, Slider, Slot, Switch, Tabs, Toast, Toggle, Toggle Group, and Tooltip.

### Styling Utilities

| Package | Description | License |
|---------|-------------|---------|
| [class-variance-authority](https://github.com/joe-bell/cva) | CSS class composition utility | Apache-2.0 |
| [clsx](https://github.com/lukeed/clsx) | Utility for constructing CSS class strings | MIT |
| [tailwind-merge](https://github.com/dcastil/tailwind-merge) | Merge Tailwind CSS classes without conflicts | MIT |

### UI Libraries

| Package | Description | License |
|---------|-------------|---------|
| [cmdk](https://github.com/pacocoursey/cmdk) | Fast, composable command menu for React | MIT |
| [Recharts](https://github.com/recharts/recharts) | Composable charting library built on React components | MIT |
| [React Day Picker](https://github.com/gpbl/react-day-picker) | Flexible date picker component for React | MIT |
| [Puck Editor](https://github.com/measuredco/puck) | Visual editor for React | MIT |
| [QRCode](https://github.com/soldair/node-qrcode) | QR code generator | MIT |

### Code Quality

| Package | Description | License |
|---------|-------------|---------|
| [Biome](https://github.com/biomejs/biome) | Fast formatter and linter for JavaScript and TypeScript | MIT |

### Testing

| Package | Description | License |
|---------|-------------|---------|
| [Playwright](https://github.com/microsoft/playwright) | End-to-end testing framework for web applications | Apache-2.0 |
| [Faker.js](https://github.com/faker-js/faker) | Generate realistic fake data for testing | MIT |

## Special Thanks

This project would not be possible without the vibrant open-source ecosystems of .NET and JavaScript/TypeScript. We extend our sincere gratitude to every contributor who has made these tools available.

If you believe a package is missing from this list, please open an issue or submit a pull request.
