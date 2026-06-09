using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Extensions;
using SimpleModule.FileStorage.Contracts;

namespace SimpleModule.FileStorage.Endpoints.Files;

public class UploadEndpoint : IEndpoint
{
    public const string Route = FileStorageConstants.Routes.Upload;
    public const string Method = "POST";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                Route,
                async Task<IResult> (
                    IFormFile? file,
                    string? folder,
                    HttpContext context,
                    IFileStorageContracts files,
                    IOptions<FileStorageModuleOptions> options
                ) =>
                {
                    if (file is null || file.Length == 0)
                    {
                        return TypedResults.BadRequest("A file is required.");
                    }

                    var maxBytes = options.Value.MaxFileSizeMb * 1024L * 1024L;
                    if (file.Length > maxBytes)
                    {
                        return TypedResults.Problem(
                            detail: $"File exceeds the maximum allowed size of "
                                + $"{options.Value.MaxFileSizeMb} MB.",
                            statusCode: StatusCodes.Status413PayloadTooLarge
                        );
                    }

                    // An empty AllowedExtensions list means "no restriction".
                    var allowedExtensions = options
                        .Value.AllowedExtensions.Split(
                            ',',
                            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                        )
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var extension = Path.GetExtension(file.FileName);
                    if (
                        allowedExtensions.Count > 0
                        && (
                            string.IsNullOrEmpty(extension)
                            || !allowedExtensions.Contains(extension)
                        )
                    )
                    {
                        return TypedResults.BadRequest(
                            $"File type '{extension}' is not allowed. Allowed extensions: "
                                + $"{options.Value.AllowedExtensions}"
                        );
                    }

                    var userId = context.User.GetUserId();
                    await using var stream = file.OpenReadStream();
                    var storedFile = await files.UploadFileAsync(
                        stream,
                        file.FileName,
                        file.ContentType,
                        folder,
                        userId
                    );
                    return TypedResults.Created($"/api/files/{storedFile.Id}", storedFile);
                }
            )
            .RequirePermission(FileStoragePermissions.Upload)
            .DisableAntiforgery();
}
