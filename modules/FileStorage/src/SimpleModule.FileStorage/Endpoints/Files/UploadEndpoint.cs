using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.FileStorage.Contracts;

namespace SimpleModule.FileStorage.Endpoints.Files;

public class UploadEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                "/",
                async Task<IResult> (
                    IFormFile? file,
                    string? folder,
                    IFileStorageContracts files
                ) =>
                {
                    if (file is null || file.Length == 0)
                    {
                        return TypedResults.BadRequest("A file is required.");
                    }

                    await using var stream = file.OpenReadStream();
                    var storedFile = await files.UploadFileAsync(
                        stream,
                        file.FileName,
                        file.ContentType,
                        folder
                    );
                    return TypedResults.Created($"/api/files/{storedFile.Id}", storedFile);
                }
            )
            .RequirePermission(FileStoragePermissions.Upload)
            .DisableAntiforgery();
}
