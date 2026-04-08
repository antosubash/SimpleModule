using SimpleModule.Core;

namespace SimpleModule.Map.Contracts;

[Dto]
public class CreateLayerSourceFromDatasetRequest
{
    public Guid DatasetId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}
