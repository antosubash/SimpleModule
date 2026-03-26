using Vogen;

namespace SimpleModule.FileStorage.Contracts;

[ValueObject<int>(conversions: Conversions.SystemTextJson | Conversions.EfCoreValueConverter)]
public readonly partial struct FileStorageId;
