using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Branding.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Extensions;
using SimpleModule.Core.Settings;
using SimpleModule.FileStorage.Contracts;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Branding.Endpoints.Branding;

public class UploadAssetEndpoint : IEndpoint
{
    private const long MaxBytes = 2 * 1024 * 1024;

    // Raster + icon formats only. SVG is intentionally excluded: it is served
    // anonymously and can carry inline script that executes on direct navigation
    // (stored XSS). Raster formats cannot.
    private static readonly HashSet<string> AllowedContentTypes = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        "image/png",
        "image/jpeg",
        "image/gif",
        "image/webp",
        "image/x-icon",
        "image/vnd.microsoft.icon",
    };

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                "/api/branding/assets/{kind}",
                async Task<IResult> (
                    string kind,
                    IFormFile? file,
                    HttpContext context,
                    IFileStorageContracts files,
                    ISettingsContracts settings
                ) =>
                {
                    if (kind is not ("logo" or "favicon"))
                        return TypedResults.BadRequest("Invalid asset kind.");
                    if (file is null || file.Length == 0)
                        return TypedResults.BadRequest("A file is required.");
                    if (!AllowedContentTypes.Contains(file.ContentType))
                        return TypedResults.BadRequest(
                            "Unsupported image type. Allowed: PNG, JPEG, GIF, WebP, ICO."
                        );
                    if (file.Length > MaxBytes)
                        return TypedResults.BadRequest("File too large (max 2 MB).");

                    var userId = context.User.GetUserId();
                    await using var stream = file.OpenReadStream();
                    var stored = await files.UploadFileAsync(
                        stream,
                        file.FileName,
                        file.ContentType,
                        "branding",
                        userId
                    );

                    var key =
                        kind == "logo"
                            ? BrandingSettingKeys.LogoFileId
                            : BrandingSettingKeys.FaviconFileId;
                    var fileId = stored.Id.Value.ToString(CultureInfo.InvariantCulture);
                    await settings.SetSettingAsync(
                        key,
                        JsonSerializer.SerializeToElement(fileId),
                        SettingScope.Application
                    );

                    return TypedResults.Ok(
                        new { fileId, url = $"/api/branding/assets/{kind}?v={fileId}" }
                    );
                }
            )
            .RequirePermission(BrandingPermissions.Manage)
            .DisableAntiforgery();
}
