---
outline: deep
---

# Quick Start

Get a working SimpleModule application running in under five minutes.

## Prerequisites

Before you begin, make sure you have:

- [**.NET 10 SDK**](https://dotnet.microsoft.com/download/dotnet/10.0) (or later)
- [**Node.js 20+**](https://nodejs.org/) (LTS recommended)
- **npm** (comes with Node.js)

Verify your setup:

```bash
dotnet --version    # should print 10.0.x or higher
node --version      # should print v20.x or higher
npm --version       # should print 10.x or higher
```

## Option A: Using the CLI (Recommended)

The `sm` CLI is the fastest way to get started. It scaffolds a complete solution with all the right project references, build configuration, and frontend wiring.

### Install the CLI

```bash
dotnet tool install -g SimpleModule.Cli
```

### Create a New Project

```bash
sm new project MyApp
cd MyApp
```

This generates a full solution with the host app, framework references, frontend packages, and a sample module.

### Build and Run

```bash
dotnet build
npm install
npm run dev
```

The `npm run dev` command starts everything at once:

- **ASP.NET backend** on `https://localhost:5001`
- **Vite watchers** for all module frontends (unminified, with source maps)
- **ClientApp watcher** for the main React entry point

::: tip Hot Reload
Edit any TypeScript/React file and Vite rebuilds instantly. Refresh your browser to see changes. Edit C# files and the dotnet process picks up changes via hot reload.
:::

Open [https://localhost:5001](https://localhost:5001) in your browser. You should see the SimpleModule dashboard.

## Option B: Manual Setup

If you prefer to clone the template directly:

```bash
git clone https://github.com/antosubash/SimpleModule.git MyApp
cd MyApp
dotnet build
npm install
npm run dev
```

Open [https://localhost:5001](https://localhost:5001).

## Creating Your First Module

With the CLI installed and your project running, add a new module:

```bash
sm new module Customers
```

This creates three projects following the standard module pattern:

```
src/modules/Customers/
├── src/
│   ├── Customers/                       # Module implementation
│   │   ├── Customers.csproj
│   │   ├── CustomersModule.cs           # [Module] class with ConfigureServices
│   │   ├── CustomersConstants.cs        # Module constants
│   │   ├── CustomersDbContext.cs        # EF Core DbContext
│   │   ├── CustomerService.cs           # Default ICustomerContracts implementation
│   │   ├── Endpoints/
│   │   │   └── Customers/
│   │   │       └── GetAllEndpoint.cs   # Starter endpoint
│   │   └── tsconfig.json
│   └── Customers.Contracts/             # Public interface for other modules
│       ├── Customers.Contracts.csproj
│       ├── ICustomerContracts.cs        # Contract interface
│       ├── Customer.cs                  # Shared DTO with [Dto] attribute
│       └── Events/
│           └── CustomerCreatedEvent.cs  # Contract-level event
└── tests/
    └── Customers.Tests/                 # xUnit test project
        ├── Customers.Tests.csproj
        ├── GlobalUsings.cs
        ├── Unit/CustomerServiceTests.cs
        └── Integration/CustomersEndpointTests.cs
```

The CLI also:

- Adds `ProjectReference` entries to the host app
- Registers all projects in `SimpleModule.slnx`

::: info Frontend files are added on first feature
`sm new module` creates only the C# backend and test projects. `Pages/index.ts`, `Views/`, and the frontend wiring are created the first time you run `sm new feature` against the module.
:::

### The Generated Module Class

```csharp
[Module("Customers", RoutePrefix = "customers")]
public sealed class CustomersModule : IModule
{
    public static void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<ICustomerContracts, CustomerService>();
    }
}
```

### The Generated Contract

```csharp
// In Customers.Contracts
public interface ICustomerContracts
{
    Task<List<CustomerDto>> GetAllAsync(CancellationToken cancellationToken);
}

[Dto]
public sealed record CustomerDto(int Id, string Name, string Email);
```

::: info The `[Dto]` Attribute
Marking a type with `[Dto]` tells the source generator to include it in JSON serializer context generation and TypeScript type extraction. Always use it on types that cross module boundaries.
:::

## Adding a Feature

Add a browsing feature to the Customers module:

```bash
sm new feature Browse --module Customers
```

Run `sm new feature` with no arguments for an interactive prompt that asks for the feature name, module, HTTP method, and route.

This scaffolds:

- A C# endpoint class (`Endpoints/Customers/BrowseEndpoint.cs`)
- A React page component (`Views/Browse.tsx`)
- An entry in the page registry (`Pages/index.ts`)

### The Endpoint

```csharp
public sealed class BrowseEndpoint : IViewEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/", Handler);

    private static async Task<IResult> Handler(
        ICustomerContracts customers,
        CancellationToken cancellationToken)
    {
        var items = await customers.GetAllAsync(cancellationToken);
        return Inertia.Render("Customers/Browse", new { Customers = items });
    }
}
```

### The React Page

```tsx
import { Head } from "@inertiajs/react";

interface Props {
    customers: Array<{
        id: number;
        name: string;
        email: string;
    }>;
}

export default function Browse({ customers }: Props) {
    return (
        <>
            <Head title="Customers" />
            <h1>Customers</h1>
            <ul>
                {customers.map((customer) => (
                    <li key={customer.id}>
                        {customer.name} - {customer.email}
                    </li>
                ))}
            </ul>
        </>
    );
}
```

### The Page Registry

```typescript
// src/modules/Customers/src/Customers/Pages/index.ts
export const pages: Record<string, unknown> = {
    "Customers/Browse": () => import("@/Views/Browse"),
};
```

::: danger Don't Forget the Page Registry
Every `IViewEndpoint` that calls `Inertia.Render("Customers/Something", ...)` **must** have a matching entry in `Pages/index.ts`. If you forget, the endpoint works on the server but silently 404s on the client with no error message.

Run `npm run validate-pages` to catch mismatches.
:::

## Running Tests

Run the full test suite:

```bash
dotnet test
```

Run tests for a specific module:

```bash
dotnet test --filter "FullyQualifiedName~Customers"
```

Run a single test method:

```bash
dotnet test --filter "FullyQualifiedName~BrowseCustomers_ReturnsOk"
```

The test infrastructure provides:

- **`SimpleModuleWebApplicationFactory`** -- pre-configured with in-memory SQLite and a test auth scheme
- **`CreateAuthenticatedClient()`** -- returns an `HttpClient` with auth claims injected via headers
- **`FakeDataGenerators`** -- Bogus-based fakers for all module DTOs

```csharp
public sealed class BrowseCustomersTests(
    SimpleModuleWebApplicationFactory factory) : IClassFixture<SimpleModuleWebApplicationFactory>
{
    [Fact]
    public async Task BrowseCustomers_ReturnsOk()
    {
        var client = factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/customers");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

## Docker

Run the full stack with Docker Compose:

```bash
docker compose up
```

This starts:

- The SimpleModule host app on **http://localhost:8080**
- A **PostgreSQL** instance for production-like database behavior

::: tip Development vs Customerion Database
During local development with `npm run dev`, the app uses SQLite by default -- no database server needed. Docker Compose switches to PostgreSQL to match production behavior.
:::

## Development Workflow Summary

| Command                  | What it does                                    |
| ------------------------ | ----------------------------------------------- |
| `npm run dev`            | Start backend + all frontend watchers           |
| `npm run build`          | Customerion build (minified, optimized)          |
| `npm run dev:build`      | Dev build (unminified, source maps)             |
| `npm run check`          | Lint + format check (Biome)                     |
| `npm run check:fix`      | Auto-fix lint + formatting                      |
| `npm run validate-pages` | Verify all endpoints have page registry entries |
| `dotnet test`            | Run all tests                                   |
| `dotnet build`           | Build the solution                              |
| `sm doctor --fix`        | Validate and fix project structure              |

## Next Steps

- [Project Structure](./project-structure) -- understand how the solution is organized
- [Modules](/guide/modules) -- deep dive into the module system
- [Endpoints](/guide/endpoints) -- learn about API and view endpoint patterns
