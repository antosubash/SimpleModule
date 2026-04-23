---
layout: home

hero:
    name: SimpleModule
    text: Modular Monolith Framework for .NET
    tagline: Compile-time module discovery via Roslyn source generators. React 19 + Inertia.js frontend served by ASP.NET Core.
    actions:
        - theme: brand
          text: Get Started
          link: /getting-started/introduction
        - theme: alt
          text: View on GitHub
          link: https://github.com/antosubash/SimpleModule

features:
    - icon: ⚡
      title: Compile-time Discovery
      details: Roslyn source generators scan your modules at build time — no reflection, no runtime overhead. Endpoints, DTOs, and menus are auto-registered.
    - icon: ⚛️
      title: React + Inertia.js
      details: Build modern React 19 frontends with server-driven navigation. Each module ships its own pages bundle, dynamically loaded by the host app.
    - icon: 🔒
      title: Module Isolation
      details: Each module gets its own database schema, permissions, settings, and menu entries. Cross-module communication happens through contracts and events.
    - icon: 🛠️
      title: CLI Tooling
      details: Scaffold projects, modules, and features with the sm CLI. Built-in doctor command validates your project structure and auto-fixes issues.
    - icon: 🧪
      title: Test Infrastructure
      details: Pre-built WebApplicationFactory with in-memory SQLite, test auth scheme, and Bogus data generators. Run against PostgreSQL in CI.
    - icon: 🗄️
      title: Multi-Provider Database
      details: SQLite for development, PostgreSQL or SQL Server for production. Schema isolation per module with automatic table prefix or schema management.
    - icon: 🤖
      title: AI Agents & RAG
      details: Build AI-powered features with multi-provider LLM support (Claude, OpenAI, Ollama), tool calling, and retrieval-augmented generation with vector search.
    - icon: 📁
      title: File Storage & Background Jobs
      details: Pluggable file storage (local, S3, Azure), background job scheduling with CRON support, real-time progress tracking, and admin dashboards.
    - icon: 🌐
      title: Localization
      details: Built-in i18n with embedded JSON locale files, automatic locale resolution, and a React useTranslation() hook. Supports parameter interpolation and fallback chains.
---

