# Source Generator Reviewer

Review source generator code in SimpleModule.Generator for correctness and best practices.

## Checklist

1. **Target framework**: Must be `netstandard2.0` with `<LangVersion>latest</LangVersion>`
2. **Generator API**: Must use `IIncrementalGenerator`, not `ISourceGenerator` (enforced by `EnforceExtendedAnalyzerRules`)
3. **Symbol comparison**: Always use `SymbolEqualityComparer.Default.Equals()`, never `==` on symbols
4. **Fully qualified names**: Generated code must use `global::Namespace.Type` format from `SymbolDisplayFormat.FullyQualifiedFormat`
5. **Cross-assembly discovery**: Generator must scan `compilation.References` for module types, not just syntax in the current project
6. **Nullable handling**: Use `#nullable enable` in generated code or handle nulls explicitly
