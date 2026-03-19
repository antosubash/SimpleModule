# Split Generator into Focused Emitters

**Date:** 2026-03-18
**Status:** Approved

## Problem

The `ModuleDiscovererGenerator` is a single monolithic class (split across 3 partial files) that handles all discovery and all 8 code generation outputs. This makes the codebase harder to navigate and maintain.

## Decision

**Approach B: Single discovery generator dispatching to separate emitter classes.**

Keep one `[Generator]` with one `CompilationProvider` pipeline (single discovery pass). Factor each output into its own emitter class implementing a shared `IEmitter` interface. The generator's `RegisterSourceOutput` callback iterates the emitter list.

This preserves the single-pass discovery performance while distributing complexity across focused, independently testable emitter classes.

## Structure

```
SimpleModule.Generator/
├── ModuleDiscoveryGenerator.cs            # [Generator] — discovery pipeline + dispatches to emitters
├── Discovery/
│   ├── DiscoveryData.cs                   # DiscoveryData record + all model types
│   └── SymbolDiscovery.cs                 # ExtractDiscoveryData + all Find* methods (static)
├── Emitters/
│   ├── IEmitter.cs                        # interface: void Emit(SourceProductionContext, DiscoveryData)
│   ├── DiagnosticEmitter.cs               # ReportDiscoveryDiagnostics + diagnostic descriptors
│   ├── ModuleExtensionsEmitter.cs         # ModuleExtensions.g.cs
│   ├── EndpointExtensionsEmitter.cs       # EndpointExtensions.g.cs
│   ├── MenuExtensionsEmitter.cs           # MenuExtensions.g.cs
│   ├── RazorComponentExtensionsEmitter.cs # RazorComponentExtensions.g.cs
│   ├── ViewPagesEmitter.cs               # ViewPages_{ModuleName}.g.cs (per module)
│   ├── JsonResolverEmitter.cs             # ModulesJsonResolver.g.cs
│   ├── TypeScriptDefinitionsEmitter.cs    # DtoTypeScript_{ModuleName}.g.cs (per module)
│   └── HostDbContextEmitter.cs            # HostDbContext.g.cs
├── Helpers/
│   └── TypeMappingHelpers.cs              # MapCSharpTypeToTypeScript, GetModuleFieldName, shared utils
└── IsExternalInit.cs                      # polyfill (unchanged)
```

## Key Decisions

- **`IEmitter` interface**: `void Emit(SourceProductionContext spc, DiscoveryData data)`. Each emitter implements this.
- **`SymbolDiscovery`**: Static class with pure function `Extract(Compilation) → DiscoveryData`. Contains all `Find*` methods.
- **`DiagnosticEmitter` runs first**: Reports diagnostics. `HostDbContextEmitter` checks `DiscoveryData` for error conditions itself (same as current behavior).
- **Shared helpers**: `GetModuleFieldName` and `MapCSharpTypeToTypeScript` move to `TypeMappingHelpers` since multiple emitters use them.
- **Module singleton fields**: Emitted by `ModuleExtensionsEmitter`; other emitters reference them via `ModuleExtensions.s_{name}`.

## What Doesn't Change

- Single `[Generator]` attribute on one class
- Incremental pipeline structure (single `CompilationProvider` → single `RegisterSourceOutput`)
- All generated output filenames and content (byte-identical)
- All existing tests pass without modification
- `DiscoveryData` equatable caching behavior
