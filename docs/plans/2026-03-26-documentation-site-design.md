# Documentation Site Design

**Date:** 2026-03-26
**Status:** Approved
**Framework:** VitePress
**URL:** docs.simplemodule.dev

## Overview

Public-facing documentation site for SimpleModule framework users. Markdown-based, powered by VitePress, hosted at docs.simplemodule.dev.

## Audience

Developers who install SimpleModule via NuGet/npm and build modular monolith applications with it.

## Technology Choice

VitePress — lightest, fastest, best out-of-box developer docs experience, markdown-first.

## Site Structure

```
docs/site/
├── .vitepress/config.ts       — Nav, sidebar, theme configuration
├── public/favicon.svg         — Site favicon
├── index.md                   — Landing/hero page
├── getting-started/           — Introduction, quick start, project structure
├── guide/                     — Core concepts (modules, endpoints, contracts, DB, events, permissions, menus, settings, inertia)
├── frontend/                  — React + Inertia architecture, pages, components, styling, Vite
├── cli/                       — sm command reference
├── testing/                   — Test stack, unit/integration/e2e patterns
├── advanced/                  — Source generator, type generation, interceptors, deployment
└── reference/                 — Configuration options, API reference
```

## Key Decisions

- No versioning (pre-1.0, single version)
- Built-in local search (miniSearch)
- Dark/light mode toggle
- GitHub link in header
- Edit on GitHub links per page
- Separate npm workspace in docs/site/
