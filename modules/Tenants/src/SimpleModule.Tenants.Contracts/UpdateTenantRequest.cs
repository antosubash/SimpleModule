namespace SimpleModule.Tenants.Contracts;

public class UpdateTenantRequest
{
    public string Name { get; set; } = string.Empty;
    public string? AdminEmail { get; set; }
    public string? EditionName { get; set; }
    public string? ConnectionString { get; set; }
    public DateTimeOffset? ValidUpTo { get; set; }
}
