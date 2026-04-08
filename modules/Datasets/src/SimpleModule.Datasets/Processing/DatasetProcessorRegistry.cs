using SimpleModule.Datasets.Contracts;

namespace SimpleModule.Datasets.Processing;

public sealed class DatasetProcessorRegistry(IEnumerable<IDatasetProcessor> processors)
{
    private readonly Dictionary<DatasetFormat, IDatasetProcessor> _byFormat =
        processors.ToDictionary(p => p.Format);

    public IDatasetProcessor Resolve(DatasetFormat format) =>
        _byFormat.TryGetValue(format, out var processor)
            ? processor
            : throw new InvalidOperationException($"No processor registered for format {format}");
}
