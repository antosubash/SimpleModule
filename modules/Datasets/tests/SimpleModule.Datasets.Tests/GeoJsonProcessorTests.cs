using FluentAssertions;
using SimpleModule.Datasets.Contracts;
using SimpleModule.Datasets.Processing;

namespace SimpleModule.Datasets.Tests;

public sealed class GeoJsonProcessorTests
{
    [Fact]
    public async Task ProcessAsync_Extracts_Bbox_And_Feature_Count()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample.geojson");
        await using var stream = File.OpenRead(path);
        var processor = new GeoJsonProcessor();

        var result = await processor.ProcessAsync(stream, TestContext.Current.CancellationToken);

        processor.Format.Should().Be(DatasetFormat.GeoJson);
        result.FeatureCount.Should().Be(2);
        result.BoundingBox.Should().NotBeNull();
        result.BoundingBox!.MinX.Should().Be(10.0);
        result.BoundingBox.MinY.Should().Be(20.0);
        result.BoundingBox.MaxX.Should().Be(30.0);
        result.BoundingBox.MaxY.Should().Be(40.0);
        result.NormalizedGeoJson.Should().NotBeNull();
    }
}
