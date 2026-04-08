using SimpleModule.Core;

namespace SimpleModule.Datasets.Contracts;

[Dto]
public sealed class BoundingBoxDto
{
    public double MinX { get; set; }
    public double MinY { get; set; }
    public double MaxX { get; set; }
    public double MaxY { get; set; }
}
