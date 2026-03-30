namespace SimpleModule.Tenants.Contracts;

public class CreateTenantRequest
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? AdminEmail { get; set; }
    public string? EditionName { get; set; }
    public string? ConnectionString { get; set; }
    public DateTimeOffset? ValidUpTo { get; set; }
    public List<string>? Hosts { get; set; }
}
