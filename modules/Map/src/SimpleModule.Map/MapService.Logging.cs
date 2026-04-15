using Microsoft.Extensions.Logging;
using SimpleModule.Map.Contracts;

namespace SimpleModule.Map;

public partial class MapService
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "LayerSource {Id} not found")]
    private static partial void LogLayerSourceNotFound(ILogger logger, LayerSourceId id);

    [LoggerMessage(Level = LogLevel.Information, Message = "LayerSource {Id} created: {Name}")]
    private static partial void LogLayerSourceCreated(
        ILogger logger,
        LayerSourceId id,
        string name
    );

    [LoggerMessage(Level = LogLevel.Information, Message = "LayerSource {Id} updated: {Name}")]
    private static partial void LogLayerSourceUpdated(
        ILogger logger,
        LayerSourceId id,
        string name
    );

    [LoggerMessage(Level = LogLevel.Information, Message = "LayerSource {Id} deleted")]
    private static partial void LogLayerSourceDeleted(ILogger logger, LayerSourceId id);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Default map seeded from MapModuleOptions"
    )]
    private static partial void LogDefaultMapSeeded(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Default map updated")]
    private static partial void LogDefaultMapUpdated(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Basemap {Id} not found")]
    private static partial void LogBasemapNotFound(ILogger logger, BasemapId id);

    [LoggerMessage(Level = LogLevel.Information, Message = "Basemap {Id} created: {Name}")]
    private static partial void LogBasemapCreated(ILogger logger, BasemapId id, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "Basemap {Id} updated: {Name}")]
    private static partial void LogBasemapUpdated(ILogger logger, BasemapId id, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "Basemap {Id} deleted")]
    private static partial void LogBasemapDeleted(ILogger logger, BasemapId id);
}
