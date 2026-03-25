# .NET-Native Vite File Watching

## Problem

Development requires running `npm run dev` (a Node-based orchestrator) separately from `dotnet run`. This spawns 10+ persistent Vite watcher processes that can become orphaned. The goal is to make `dotnet run` the single command for development.

## Decision

Use .NET's `FileSystemWatcher` + on-demand `npx vite build` calls instead of persistent Vite watcher processes. No Node orchestration needed.

## Architecture

A `ViteDevWatchService : BackgroundService` in the Host project:

1. **Discovers** all module directories with `vite.config.ts`
2. **Sets up `FileSystemWatcher`** instances filtering for `*.ts`, `*.tsx`, `*.css`
3. **On file change**, debounces (300ms), then runs `npx vite build` for the changed module only
4. **Also watches** `ClientApp/` for the host app
5. **Only registers in Development** — production uses existing MSBuild `JsBuild` targets
6. **Logs** rebuild events to ASP.NET's ILogger

## Key Behaviors

- **Debouncing**: Multiple rapid saves within 300ms trigger a single rebuild
- **Concurrency**: If a build is already running for a module, skip — the next save will catch it
- **No persistent Node processes** — each rebuild is a short-lived process
- **Environment**: Sets `VITE_MODE=dev` for all watcher-triggered builds
- **Tailwind**: Also triggers Tailwind rebuild when CSS files change

## File Changes

| File | Action |
|------|--------|
| `template/SimpleModule.Host/Services/ViteDevWatchService.cs` | New |
| `template/SimpleModule.Host/Program.cs` | Register service in Development |

## What This Replaces

- `npm run dev` becomes optional — `dotnet run` is sufficient
- Node dev-orchestrator kept as fallback
