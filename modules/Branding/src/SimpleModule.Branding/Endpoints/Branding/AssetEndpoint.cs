using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Settings;
using SimpleModule.FileStorage.Contracts;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Branding.Endpoints.Branding;

/// <summary>
/// Serves the branding logo/favicon anonymously. FileStorage's own download endpoint
/// is permissioned and ownership-checked, so it cannot serve assets to anonymous
/// visitors (e.g. on the login page); this endpoint intentionally can.
/// </summary>
public class AssetEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/api/branding/assets/{kind}",
                async Task<IResult> (
                    string kind,
                    HttpContext context,
                    ISettingsContracts settings,
                    IFileStorageContracts files
                ) =>
                {
                    if (!BrandingAssets.IsValidKind(kind))
                        return Results.NotFound();

                    var idStr = await settings.GetSettingAsync<string>(
                        BrandingAssets.SettingKey(kind),
                        SettingScope.Application
                    );
                    if (
                        string.IsNullOrWhiteSpace(idStr)
                        || !int.TryParse(
                            idStr,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out var id
                        )
                    )
                        return Results.NotFound();

                    var file = await files.GetFileByIdAsync(FileStorageId.From(id));
                    if (file is null)
                        return Results.NotFound();

                    // Re-validate the stored content type on the serve path, not just at
                    // upload: the file id lives in a Text setting that the generic Settings
                    // admin UI can repoint at ANY FileStorage id (e.g. an SVG uploaded via
                    // FileStorage). Serving a non-raster type anonymously would reintroduce
                    // the SVG stored-XSS vector that the upload allowlist guards against.
                    if (!BrandingAssets.AllowedContentTypes.Contains(file.ContentType))
                        return Results.NotFound();

                    var stream = await files.DownloadFileAsync(file);
                    if (stream is null)
                        return Results.NotFound();

                    context.Response.Headers.CacheControl = "public, max-age=3600";
                    context.Response.Headers.XContentTypeOptions = "nosniff";
                    return Results.File(stream, file.ContentType);
                }
            )
            .AllowAnonymous();
}
