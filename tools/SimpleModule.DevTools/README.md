# SimpleModule.DevTools

[![NuGet](https://img.shields.io/nuget/v/SimpleModule.DevTools.svg)](https://www.nuget.org/packages/SimpleModule.DevTools)

Development-time tooling for SimpleModule, including Vite dev watch and diagnostics.

## Installation

```bash
dotnet add package SimpleModule.DevTools
```

## Quick Start

DevTools is automatically included when you reference SimpleModule.Hosting. During development, it provides:

```bash
npm run dev
# Starts ASP.NET backend + Vite watch for all modules
```

## Key Features

- **Vite dev watch service** -- auto-rebuilds module frontend assets on file changes
- **Diagnostics** -- development-time health checks and module status reporting
- **Hot reload support** -- works with .NET hot reload and Vite HMR

## Links

- [GitHub Repository](https://github.com/antosubash/SimpleModule)
