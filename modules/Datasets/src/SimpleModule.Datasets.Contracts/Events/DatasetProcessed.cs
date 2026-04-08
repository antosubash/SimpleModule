using SimpleModule.Core.Events;

namespace SimpleModule.Datasets.Contracts.Events;

public sealed record DatasetProcessed(DatasetId DatasetId, DatasetStatus Status) : IEvent;

public sealed record DatasetDerivativeCreated(DatasetId DatasetId, DatasetFormat Format) : IEvent;
