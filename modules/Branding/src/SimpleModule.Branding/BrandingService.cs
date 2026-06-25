using System.Text.Json;
using SimpleModule.Branding.Contracts;
using SimpleModule.Core.Settings;
using SimpleModule.Settings.Contracts;

namespace SimpleModule.Branding;

/// <summary>
/// Resolves branding settings into the DTOs consumed by the shared-prop middleware,
/// the head contributor, and the admin page. Storage is the Settings module — this
/// module owns no database. Registered scoped, so the resolved model is memoized for
/// the lifetime of a request (the middleware and the head contributor both resolve
/// branding during a single page render).
/// </summary>
public sealed class BrandingService(ISettingsContracts settings) : IBrandingContracts
{
    private BrandingEditModel? _model;

    public async Task<BrandingDto> GetBrandingAsync()
    {
        var m = await ResolveAsync();
        return new BrandingDto
        {
            AppName = m.AppName,
            ColorPrimary = m.ColorPrimary,
            ColorPrimaryDark = m.ColorPrimaryDark,
            LogoUrl = m.LogoUrl,
            FaviconUrl = m.FaviconUrl,
            TopBar = m.TopBar,
            Footer = m.Footer,
        };
    }

    public async Task<string> GetCustomCssAsync() => (await ResolveAsync()).CustomCss;

    public async Task<BrandingEditModel> GetEditableAsync() => await ResolveAsync();

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
        _model = null; // invalidate per-request memo after a write
    }

    /// <summary>Reads every branding setting once and memoizes for this request.</summary>
    private async Task<BrandingEditModel> ResolveAsync()
    {
        if (_model is not null)
            return _model;

        var logoId = await settings.GetSettingAsync<string>(
            BrandingSettingKeys.LogoFileId,
            SettingScope.Application
        );
        var faviconId = await settings.GetSettingAsync<string>(
            BrandingSettingKeys.FaviconFileId,
            SettingScope.Application
        );

        _model = new BrandingEditModel
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
        return _model;
    }

    private async Task<string> Str(string key, string fallback)
    {
        var value = await settings.GetSettingAsync<string>(key, SettingScope.Application);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private Task<T?> Json<T>(string key) =>
        settings.GetSettingAsync<T>(key, SettingScope.Application);

    private static string? AssetUrl(string kind, string? fileId) =>
        string.IsNullOrWhiteSpace(fileId) ? null : BrandingAssets.Url(kind, fileId);

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
