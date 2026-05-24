namespace SimpleModule.Generator;

internal readonly record struct FormRequestInfoRecord(
    string FullyQualifiedName,
    bool IsSealed,
    bool ExtendsFormRequest,
    SourceLocationRecord? Location
);
