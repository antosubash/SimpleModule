using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Datasets.Contracts;

namespace SimpleModule.Datasets.Endpoints.Datasets;

public class ConvertDatasetEndpoint : IEndpoint
{
    public const string Route = DatasetsConstants.Routes.Convert;
    public const string Method = "POST";

    public sealed class ConvertRequest
    {
        public string? TargetFormat { get; set; }
    }

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                Route,
                async Task<IResult> (
                    Guid id,
                    ConvertRequest? request,
                    IDatasetsContracts datasets,
                    CancellationToken ct
                ) =>
                {
                    DatasetFormat? target = null;
                    if (!string.IsNullOrWhiteSpace(request?.TargetFormat))
                    {
                        if (
                            !Enum.TryParse<DatasetFormat>(
                                request.TargetFormat,
                                ignoreCase: true,
                                out var parsed
                            )
                        )
                        {
                            return TypedResults.BadRequest(
                                $"Unknown target format: {request.TargetFormat}"
                            );
                        }
                        target = parsed;
                    }

                    try
                    {
                        await datasets.EnqueueConversionAsync(DatasetId.From(id), target, ct);
                        return TypedResults.Accepted(
                            new Uri($"/api/datasets/{id}", UriKind.Relative)
                        );
                    }
                    catch (InvalidOperationException ex)
                    {
                        return TypedResults.NotFound(ex.Message);
                    }
                }
            )
            .RequirePermission(DatasetsPermissions.Convert);
}
