using Vogen;

namespace SimpleModule.Datasets.Contracts;

[ValueObject<Guid>(conversions: Conversions.SystemTextJson | Conversions.EfCoreValueConverter)]
public readonly partial struct DatasetId;
