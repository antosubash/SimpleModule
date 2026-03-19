using SimpleModule.Core;

namespace SimpleModule.PageBuilder.Contracts;

[Dto]
public class UpdatePageRequest
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsPublished { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    public string? OgImage { get; set; }
}
