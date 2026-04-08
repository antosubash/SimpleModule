namespace SimpleModule.Datasets.Contracts;

public enum DatasetFormat
{
    Unknown = 0,
    GeoJson = 1,
    Shapefile = 2,
    Kml = 3,
    Kmz = 4,
    GeoPackage = 5,
    PmTiles = 6,
    Cog = 7,
}

public static class DatasetFormatExtensions
{
    public static bool IsVector(this DatasetFormat format) =>
        format
            is DatasetFormat.GeoJson
                or DatasetFormat.Shapefile
                or DatasetFormat.Kml
                or DatasetFormat.Kmz
                or DatasetFormat.GeoPackage;

    public static bool IsRaster(this DatasetFormat format) => format is DatasetFormat.Cog;

    public static bool IsTileSource(this DatasetFormat format) => format is DatasetFormat.PmTiles;

    public static DatasetFormat FromFileName(string fileName)
    {
        var ext = System.IO.Path.GetExtension(fileName).ToUpperInvariant();
        return ext switch
        {
            ".GEOJSON" or ".JSON" => DatasetFormat.GeoJson,
            ".ZIP" or ".SHP" => DatasetFormat.Shapefile,
            ".KML" => DatasetFormat.Kml,
            ".KMZ" => DatasetFormat.Kmz,
            ".GPKG" => DatasetFormat.GeoPackage,
            ".PMTILES" => DatasetFormat.PmTiles,
            ".TIF" or ".TIFF" => DatasetFormat.Cog,
            _ => DatasetFormat.Unknown,
        };
    }

    public static string FileExtension(this DatasetFormat format) =>
        format switch
        {
            DatasetFormat.GeoJson => ".geojson",
            DatasetFormat.Shapefile => ".zip",
            DatasetFormat.Kml => ".kml",
            DatasetFormat.Kmz => ".kmz",
            DatasetFormat.GeoPackage => ".gpkg",
            DatasetFormat.PmTiles => ".pmtiles",
            DatasetFormat.Cog => ".tif",
            _ => ".bin",
        };
}
