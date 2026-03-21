namespace SimpleModule.PageBuilder.Contracts;

public class PageTemplate
{
    public PageTemplateId Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
