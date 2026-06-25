namespace SimpleModule.Branding.Contracts;

public interface IBrandingContracts
{
    Task<BrandingDto> GetBrandingAsync();
    Task<string> GetCustomCssAsync();
    Task<BrandingEditModel> GetEditableAsync();
    Task UpdateAsync(BrandingEditModel model);
}
