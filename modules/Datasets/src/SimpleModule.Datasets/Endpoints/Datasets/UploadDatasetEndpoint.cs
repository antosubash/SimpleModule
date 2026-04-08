using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Datasets.Contracts;

namespace SimpleModule.Datasets.Endpoints.Datasets;

public class UploadDatasetEndpoint : IEndpoint
{
    public const string Route = DatasetsConstants.Routes.Upload;
    public const string Method = "POST";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                Route,
                async Task<IResult> (
                    [FromForm] IFormFile? file,
                    [FromForm] string? name,
                    IDatasetsContracts datasets,
                    CancellationToken ct
                ) =>
                {
                    if (file is null || file.Length == 0)
                    {
                        return TypedResults.BadRequest("A file is required.");
                    }

                    await using var stream = file.OpenReadStream();
                    try
                    {
                        var dataset = await datasets.CreateAsync(stream, file.FileName, name, ct);
                        return TypedResults.Created($"/api/datasets/{dataset.Id}", dataset);
                    }
                    catch (InvalidOperationException ex)
                    {
                        return TypedResults.BadRequest(ex.Message);
                    }
                }
            )
            .RequirePermission(DatasetsPermissions.Upload)
            .DisableAntiforgery();
}
