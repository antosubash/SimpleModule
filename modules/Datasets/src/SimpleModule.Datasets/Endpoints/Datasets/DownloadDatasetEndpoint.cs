using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Datasets.Contracts;

namespace SimpleModule.Datasets.Endpoints.Datasets;

public class DownloadDatasetEndpoint : IEndpoint
{
    public const string Route = DatasetsConstants.Routes.Download;
    public const string Method = "GET";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                Route,
                async Task<IResult> (
                    Guid id,
                    string? variant,
                    IDatasetsContracts datasets,
                    CancellationToken ct
                ) =>
                {
                    var datasetId = DatasetId.From(id);
                    var row = await datasets.GetByIdAsync(datasetId, ct);
                    if (row is null)
                    {
                        return TypedResults.NotFound();
                    }

                    Stream? stream;
                    string fileName;
                    if (string.IsNullOrWhiteSpace(variant) || variant == "original")
                    {
                        stream = await datasets.GetOriginalAsync(datasetId, ct);
                        fileName = row.OriginalFileName;
                    }
                    else if (Enum.TryParse<DatasetFormat>(variant, ignoreCase: true, out var fmt))
                    {
                        stream = await datasets.GetDerivativeAsync(datasetId, fmt, ct);
                        fileName = $"{row.Name}{fmt.FileExtension()}";
                    }
                    else
                    {
                        return TypedResults.BadRequest(
                            "variant must be 'original' or a DatasetFormat name"
                        );
                    }

                    if (stream is null)
                    {
                        return TypedResults.NotFound();
                    }
                    return TypedResults.File(stream, "application/octet-stream", fileName);
                }
            )
            .RequirePermission(DatasetsPermissions.View);
}
