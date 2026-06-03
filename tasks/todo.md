# CLI Bug Fixes (GitHub issues)

Branch: claude/cli-bug-fixes-2OKyG

- [x] #218 Generated event records implement `IEvent` → now derive from `DomainEvent` (FallbackEventClass)
- [x] #225 `sm new project` references `/favicon.svg` but ships none → embed + write `wwwroot/favicon.svg`
- [x] #219 Scaffold pinned `@simplemodule/*` npm deps to framework (NuGet) version → new `NpmVersionResolver` resolves the latest *published* npm version; threaded through `ProjectTemplates`/`ScaffoldProject`
- [x] #228 Module `AssemblyName` = `SimpleModule.X` but dir basename = `X` → bundle 404. Set module AssemblyName to bare `X` AND contracts to `X.Contracts` (required so the generator's `module + ".Contracts"` pairing still discovers contract impls — otherwise SM0025)
- [x] #221 `SimpleModule.Hosting.targets` undefined `$(RepoRoot)` + monorepo paths → added RepoRoot fallback (nearest package.json) + overridable `SimpleModuleThemeCss`/`SimpleModuleModulesDir`/`SimpleModuleRoutesOutput` props with consumer defaults; monorepo overrides them in `Directory.Build.props` (scoped via `packages/` check)

## Verification
- [x] dotnet build CLI — succeeds
- [x] dotnet test CLI tests — 136/136 pass (incl. scaffold `dotnet build` against published NuGet, now green)
- [x] dotnet build template host — succeeds; integrated Vite + Tailwind build runs (validates #221 targets)

## Review
- #228 root cause was subtler than the issue described: bare module AssemblyName alone breaks
  the generator's module↔contracts pairing (ContractFinder uses `moduleAssembly.Name + ".Contracts"`).
  Both assembly names must go bare together; namespaces stay `SimpleModule.X(.Contracts)`.
- #219 falls back to the framework version when the npm registry is unreachable (offline),
  preserving deterministic test behavior (ScaffoldProject's npmVersion defaults to frameworkVersion).
