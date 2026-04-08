using SimpleModule.Datasets.Contracts;

namespace SimpleModule.Datasets.Processing;

// Placeholder processors for formats that require third-party libraries we have not yet
// wired into the build. They register via DI so DatasetProcessorRegistry can resolve every
// DatasetFormat, and throw a clear error when invoked. Replace with real implementations
// (NetTopologySuite.IO.Esri for Shapefile, SharpKml for KML, etc.) as the dependencies land.

public abstract class StubProcessor(DatasetFormat format, string library) : IDatasetProcessor
{
    public DatasetFormat Format { get; } = format;

    private readonly string _library = library;

    public Task<DatasetProcessingResult> ProcessAsync(Stream content, CancellationToken ct) =>
        throw new NotSupportedException(
            $"{Format} processing is not yet implemented. Add {_library} to SimpleModule.Datasets.csproj and replace this stub."
        );
}

public sealed class ShapefileProcessor()
    : StubProcessor(DatasetFormat.Shapefile, "NetTopologySuite.IO.Esri");

public sealed class KmlProcessor() : StubProcessor(DatasetFormat.Kml, "SharpKml.Core");

public sealed class GeoPackageProcessor()
    : StubProcessor(DatasetFormat.GeoPackage, "Microsoft.Data.Sqlite + NetTopologySuite.IO");

public sealed class PmTilesProcessor()
    : StubProcessor(DatasetFormat.PmTiles, "a PMTiles header reader");

public sealed class CogProcessor() : StubProcessor(DatasetFormat.Cog, "BitMiracle.LibTiff.NET");
