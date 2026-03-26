# @simplemodule/client

[![npm](https://img.shields.io/npm/v/@simplemodule/client.svg)](https://www.npmjs.com/package/@simplemodule/client)

Vite plugin and utilities for building SimpleModule frontend modules.

## Installation

```bash
npm install @simplemodule/client
```

## Quick Start

```typescript
// vite.config.ts
import { defineModuleConfig } from '@simplemodule/client/module';

export default defineModuleConfig({
  moduleName: 'Products',
});
```

```typescript
// app.tsx
import { resolvePage } from '@simplemodule/client/resolve-page';

createInertiaApp({
  resolve: resolvePage,
  // ...
});
```

## Key Features

- **defineModuleConfig** -- Vite configuration factory for module library builds
- **resolvePage** -- dynamic page resolver that loads module bundles by route name
- **Vite vendor plugin** -- bundles shared dependencies (React, Inertia) for module consumption

## Links

- [GitHub Repository](https://github.com/antosubash/SimpleModule)
