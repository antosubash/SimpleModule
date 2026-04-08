using SimpleModule.Datasets.Contracts;

namespace SimpleModule.Datasets.Converters;

public interface IDatasetConverter
{
    DatasetFormat TargetFormat { get; }
    bool CanConvertFrom(DatasetFormat source);
    Task<Stream> ConvertAsync(Stream source, DatasetFormat sourceFormat, CancellationToken ct);
}

public sealed class DatasetConverterRegistry(IEnumerable<IDatasetConverter> converters)
{
    private readonly IDatasetConverter[] _converters = [.. converters];

    public IDatasetConverter Resolve(DatasetFormat source, DatasetFormat target)
    {
        var converter = _converters.FirstOrDefault(c =>
            c.TargetFormat == target && c.CanConvertFrom(source)
        );
        return converter
            ?? throw new NotSupportedException($"No converter registered for {source} → {target}");
    }
}
