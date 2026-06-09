using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
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

    public void Map(IEndpointRouteBuilder app)
    {
        // FileStorageModuleOptions is an IOptions singleton, so the size limit and
        // the parsed extension allowlist are resolved once at registration time
        // and captured — not re-split into a new HashSet on every upload request.
        var options = app
            .ServiceProvider.GetRequiredService<IOptions<FileStorageModuleOptions>>()
            .Value;
        var maxBytes = options.MaxFileSizeMb * 1024L * 1024L;
        // An empty allowlist means "no restriction".
        var allowedExtensions = options
            .AllowedExtensions.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            )
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        app.MapPost(
                Route,
                async Task<IResult> (
                    IFormFile? file,
                    string? folder,
                    HttpContext context,
                    IFileStorageContracts files
                ) =>
                {
                    if (file is null || file.Length == 0)
                    {
                        return TypedResults.BadRequest("A file is required.");
                    }

                    if (file.Length > maxBytes)
                    {
                        return TypedResults.Problem(
                            detail: $"File exceeds the maximum allowed size of "
                                + $"{options.MaxFileSizeMb} MB.",
                            statusCode: StatusCodes.Status413PayloadTooLarge
                        );
                    }

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
                                + $"{options.AllowedExtensions}"
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
}
