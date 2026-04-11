using SimpleModule.Core.Entities;

namespace SimpleModule.PageBuilder.Contracts;

public class PageTemplate : Entity<PageTemplateId>
{
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
