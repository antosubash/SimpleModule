# SimpleModule.Database

[![NuGet](https://img.shields.io/nuget/v/SimpleModule.Database.svg)](https://www.nuget.org/packages/SimpleModule.Database)

Multi-provider database support for SimpleModule with schema isolation per module.

## Installation

```bash
dotnet add package SimpleModule.Database
```

## Quick Start

```csharp
public sealed class ProductsDbContext : ModuleDbContext<ProductsDbContext>
{
    public DbSet<Product> Products => Set<Product>();
}
```

## Key Features

- **Schema isolation** -- each module gets its own database schema (or table prefix for SQLite)
- **SQLite** support for development and lightweight deployments
- **PostgreSQL** support for production workloads
- **SQL Server** support for enterprise environments
- **EF Core** integration with per-module DbContext registration

## Links

- [GitHub Repository](https://github.com/antosubash/SimpleModule)
