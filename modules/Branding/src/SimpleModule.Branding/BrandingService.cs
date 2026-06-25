using System.Text.Json;
using SimpleModule.Branding.Contracts;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Branding;

/// <summary>
/// Resolves branding settings into the DTOs consumed by the shared-prop middleware,
/// the head contributor, and the admin page. Storage is the Settings module — this
/// module owns no database.
/// </summary>
public sealed class BrandingService(ISettingsContracts settings) : IBrandingContracts
{
    public async Task<BrandingDto> GetBrandingAsync()
    {
        var logoId = await settings.GetSettingAsync<string>(
            BrandingSettingKeys.LogoFileId,
            SettingScope.Application
        );
        var faviconId = await settings.GetSettingAsync<string>(
            BrandingSettingKeys.FaviconFileId,
            SettingScope.Application
        );

        return new BrandingDto
        {
            AppName = await Str(BrandingSettingKeys.AppName, BrandingDefaults.AppName),
            ColorPrimary = await Str(
                BrandingSettingKeys.ColorPrimary,
                BrandingDefaults.ColorPrimary
            ),
            ColorPrimaryDark = await Str(
                BrandingSettingKeys.ColorPrimaryDark,
                BrandingDefaults.ColorPrimaryDark
            ),
            LogoUrl = AssetUrl("logo", logoId),
            FaviconUrl = AssetUrl("favicon", faviconId),
            TopBar = await Json<TopBarConfig>(BrandingSettingKeys.TopBar) ?? new TopBarConfig(),
            Footer = await Json<FooterConfig>(BrandingSettingKeys.Footer) ?? new FooterConfig(),
        };
    }

    public async Task<string> GetCustomCssAsync() =>
        await Str(BrandingSettingKeys.CustomCss, string.Empty);

    public async Task<BrandingEditModel> GetEditableAsync()
    {
        var logoId = await settings.GetSettingAsync<string>(
            BrandingSettingKeys.LogoFileId,
            SettingScope.Application
        );
        var faviconId = await settings.GetSettingAsync<string>(
            BrandingSettingKeys.FaviconFileId,
            SettingScope.Application
        );

        return new BrandingEditModel
        {
            AppName = await Str(BrandingSettingKeys.AppName, BrandingDefaults.AppName),
            ColorPrimary = await Str(
                BrandingSettingKeys.ColorPrimary,
                BrandingDefaults.ColorPrimary
            ),
            ColorPrimaryDark = await Str(
                BrandingSettingKeys.ColorPrimaryDark,
                BrandingDefaults.ColorPrimaryDark
            ),
            CustomCss = await Str(BrandingSettingKeys.CustomCss, string.Empty),
            LogoFileId = string.IsNullOrWhiteSpace(logoId) ? null : logoId,
            LogoUrl = AssetUrl("logo", logoId),
            FaviconFileId = string.IsNullOrWhiteSpace(faviconId) ? null : faviconId,
            FaviconUrl = AssetUrl("favicon", faviconId),
            TopBar = await Json<TopBarConfig>(BrandingSettingKeys.TopBar) ?? new TopBarConfig(),
            Footer = await Json<FooterConfig>(BrandingSettingKeys.Footer) ?? new FooterConfig(),
        };
    }

    public async Task UpdateAsync(BrandingEditModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var updates = new List<BulkSettingUpdate>
        {
            StrUpdate(BrandingSettingKeys.AppName, model.AppName ?? BrandingDefaults.AppName),
            StrUpdate(
                BrandingSettingKeys.ColorPrimary,
                model.ColorPrimary ?? BrandingDefaults.ColorPrimary
            ),
            StrUpdate(
                BrandingSettingKeys.ColorPrimaryDark,
                model.ColorPrimaryDark ?? BrandingDefaults.ColorPrimaryDark
            ),
            StrUpdate(BrandingSettingKeys.CustomCss, model.CustomCss ?? string.Empty),
            JsonUpdate(BrandingSettingKeys.TopBar, model.TopBar ?? new TopBarConfig()),
            JsonUpdate(BrandingSettingKeys.Footer, model.Footer ?? new FooterConfig()),
        };

        // File ids are normally set by the upload endpoint; include them here so the
        // form can also clear them (empty string).
        if (model.LogoFileId is not null)
            updates.Add(StrUpdate(BrandingSettingKeys.LogoFileId, model.LogoFileId));
        if (model.FaviconFileId is not null)
            updates.Add(StrUpdate(BrandingSettingKeys.FaviconFileId, model.FaviconFileId));

        await settings.SetManyAsync(updates);
    }

    private async Task<string> Str(string key, string fallback)
    {
        var value = await settings.GetSettingAsync<string>(key, SettingScope.Application);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private Task<T?> Json<T>(string key) =>
        settings.GetSettingAsync<T>(key, SettingScope.Application);

    private static string? AssetUrl(string kind, string? fileId) =>
        string.IsNullOrWhiteSpace(fileId) ? null : $"/api/branding/assets/{kind}?v={fileId}";

    private static BulkSettingUpdate StrUpdate(string key, string value) =>
        new()
        {
            Key = key,
            Scope = SettingScope.Application,
            Value = JsonSerializer.SerializeToElement(value),
        };

    private static BulkSettingUpdate JsonUpdate<T>(string key, T value) =>
        new()
        {
            Key = key,
            Scope = SettingScope.Application,
            Value = JsonSerializer.SerializeToElement(value),
        };
}
