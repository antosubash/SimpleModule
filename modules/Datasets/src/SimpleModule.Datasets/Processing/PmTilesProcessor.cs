using System.Buffers.Binary;
using System.Diagnostics;
using System.Text.Json;
using SimpleModule.Datasets.Contracts;

namespace SimpleModule.Datasets.Processing;

/// <summary>
/// Extracts metadata from a PMTiles archive by reading the binary header.
/// PMTiles v3 header spec: https://github.com/protomaps/PMTiles/blob/main/spec/v3/spec.md
///
/// Header layout (127 bytes total):
///   0-6   : magic "PMTiles" (7 bytes UTF-8)
///   7     : version (uint8)
///   8-15  : root_dir_offset (uint64 LE)
///   16-23 : root_dir_length (uint64 LE)
///   24-31 : metadata_offset (uint64 LE)
///   32-39 : metadata_length (uint64 LE)
///   40-47 : leaf_dirs_offset (uint64 LE)
///   48-55 : leaf_dirs_length (uint64 LE)
///   56-63 : tile_data_offset (uint64 LE)
///   64-71 : tile_data_length (uint64 LE)
///   72-79 : num_addressed_tiles (uint64 LE)
///   80-87 : num_tile_entries (uint64 LE)
///   88-95 : num_tile_contents (uint64 LE)
///   96    : clustered (uint8)
///   97    : internal_compression (uint8)
///   98    : tile_compression (uint8)
///   99    : tile_type (uint8)
///   100   : min_zoom (uint8)
///   101   : max_zoom (uint8)
///   102-105: min_lon_e7 (int32 LE)
///   106-109: min_lat_e7 (int32 LE)
///   110-113: max_lon_e7 (int32 LE)
///   114-117: max_lat_e7 (int32 LE)
///   118   : center_zoom (uint8)
///   119-122: center_lon_e7 (int32 LE)
///   123-126: center_lat_e7 (int32 LE)
/// </summary>
public sealed class PmTilesProcessor : IDatasetProcessor
{
    public DatasetFormat Format => DatasetFormat.PmTiles;

    private static ReadOnlySpan<byte> Magic => "PMTiles"u8;
    private const int HeaderSize = 127;

    public async Task<DatasetProcessingResult> ProcessAsync(Stream content, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        var header = new byte[HeaderSize];
        var bytesRead = await content.ReadAsync(header.AsMemory(0, HeaderSize), ct);
        if (bytesRead < HeaderSize)
        {
            throw new InvalidOperationException(
                $"PMTiles file too small: expected at least {HeaderSize} bytes, got {bytesRead}"
            );
        }

        if (!header.AsSpan(0, 7).SequenceEqual(Magic))
        {
            throw new InvalidOperationException("Not a valid PMTiles file: magic header mismatch");
        }

        var version = header[7];
        var metadataOffset = BinaryPrimitives.ReadUInt64LittleEndian(header.AsSpan(24, 8));
        var metadataLength = BinaryPrimitives.ReadUInt64LittleEndian(header.AsSpan(32, 8));
        var tileCount = (long)BinaryPrimitives.ReadUInt64LittleEndian(header.AsSpan(72, 8));
        var tileType = header[99];
        var minZoom = header[100];
        var maxZoom = header[101];
        var minLonE7 = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(102, 4));
        var minLatE7 = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(106, 4));
        var maxLonE7 = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(110, 4));
        var maxLatE7 = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(114, 4));
        var centerLonE7 = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(119, 4));
        var centerLatE7 = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(123, 4));

        var tileFormatName = tileType switch
        {
            0 => "Unknown",
            1 => "mvt",
            2 => "png",
            3 => "jpeg",
            4 => "webp",
            5 => "avif",
            _ => $"type_{tileType}",
        };

        var layerNames = new List<string>();

        if (
            metadataLength > 0
            && metadataLength < 10 * 1024 * 1024
            && metadataOffset >= HeaderSize
            && content.CanSeek
        )
        {
            try
            {
                content.Position = (long)metadataOffset;
                var metaBytes = new byte[(int)metadataLength];
                await content.ReadExactlyAsync(metaBytes, ct);

                // PMTiles metadata may be gzip-compressed; try JSON parse first
                try
                {
                    using var metaDoc = JsonDocument.Parse(metaBytes);
                    if (metaDoc.RootElement.TryGetProperty("vector_layers", out var vectorLayers))
                    {
                        foreach (var vl in vectorLayers.EnumerateArray())
                        {
                            if (vl.TryGetProperty("id", out var id))
                            {
                                layerNames.Add(id.GetString() ?? "");
                            }
                        }
                    }
                }
#pragma warning disable CA1031
                catch
                { /* gzip-compressed or invalid JSON — skip */
                }
#pragma warning restore CA1031
            }
#pragma warning disable CA1031
            catch
            { /* seek failed — skip metadata parsing */
            }
#pragma warning restore CA1031
        }

        var bbox = new BoundingBoxDto
        {
            MinX = minLonE7 / 10_000_000.0,
            MinY = minLatE7 / 10_000_000.0,
            MaxX = maxLonE7 / 10_000_000.0,
            MaxY = maxLatE7 / 10_000_000.0,
        };

        var metadata = new DatasetMetadata
        {
            Common = new CommonMetadata
            {
                SourceFormat = nameof(DatasetFormat.PmTiles),
                SourceSrid = 4326,
                TargetSrid = 4326,
                BoundingBox = bbox,
                ProcessingDurationMs = sw.Elapsed.TotalMilliseconds,
            },
            Tiles = new TileMetadata
            {
                TileFormat = tileFormatName,
                MinZoom = minZoom,
                MaxZoom = maxZoom,
                CenterLon = centerLonE7 / 10_000_000.0,
                CenterLat = centerLatE7 / 10_000_000.0,
                TileCount = tileCount,
                HeaderVersion = version,
                LayerNames = layerNames,
            },
        };

        return new DatasetProcessingResult
        {
            SourceSrid = 4326,
            TargetSrid = 4326,
            BoundingBox = bbox,
            FeatureCount = null,
            Metadata = metadata,
            NormalizedGeoJson = null,
        };
    }
}
