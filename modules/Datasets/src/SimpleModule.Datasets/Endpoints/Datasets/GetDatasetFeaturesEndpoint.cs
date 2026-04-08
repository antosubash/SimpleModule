using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Datasets.Contracts;

namespace SimpleModule.Datasets.Endpoints.Datasets;

public class GetDatasetFeaturesEndpoint : IEndpoint
{
    public const string Route = DatasetsConstants.Routes.Features;
    public const string Method = "GET";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                async Task<IResult> (
                    Guid id,
                    string? bbox,
                    int? limit,
                    IDatasetsContracts datasets,
                    CancellationToken ct
                ) =>
                {
                    BoundingBoxDto? parsed = null;
                    if (!string.IsNullOrWhiteSpace(bbox))
                    {
                        var parts = bbox.Split(
                            ',',
                            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                        );
                        if (
                            parts.Length != 4
                            || !double.TryParse(
                                parts[0],
                                System.Globalization.CultureInfo.InvariantCulture,
                                out var minX
                            )
                            || !double.TryParse(
                                parts[1],
                                System.Globalization.CultureInfo.InvariantCulture,
                                out var minY
                            )
                            || !double.TryParse(
                                parts[2],
                                System.Globalization.CultureInfo.InvariantCulture,
                                out var maxX
                            )
                            || !double.TryParse(
                                parts[3],
                                System.Globalization.CultureInfo.InvariantCulture,
                                out var maxY
                            )
                        )
                        {
                            return TypedResults.BadRequest("bbox must be 'minX,minY,maxX,maxY'");
                        }
                        parsed = new BoundingBoxDto
                        {
                            MinX = minX,
                            MinY = minY,
                            MaxX = maxX,
                            MaxY = maxY,
                        };
                    }

                    try
                    {
                        var json = await datasets.GetFeaturesGeoJsonAsync(
                            DatasetId.From(id),
                            parsed,
                            limit,
                            ct
                        );
                        return TypedResults.Content(json, "application/geo+json");
                    }
                    catch (InvalidOperationException ex)
                    {
                        return TypedResults.BadRequest(ex.Message);
                    }
                }
            )
            .RequirePermission(DatasetsPermissions.View);
}
