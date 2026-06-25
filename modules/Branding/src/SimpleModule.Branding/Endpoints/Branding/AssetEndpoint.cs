using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Branding.Contracts;
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
                    if (kind is not ("logo" or "favicon"))
                        return Results.NotFound();

                    var key =
                        kind == "logo"
                            ? BrandingSettingKeys.LogoFileId
                            : BrandingSettingKeys.FaviconFileId;
                    var idStr = await settings.GetSettingAsync<string>(
                        key,
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

                    var stream = await files.DownloadFileAsync(file);
                    if (stream is null)
                        return Results.NotFound();

                    context.Response.Headers.CacheControl = "public, max-age=3600";
                    return Results.File(stream, file.ContentType);
                }
            )
            .AllowAnonymous();
}
