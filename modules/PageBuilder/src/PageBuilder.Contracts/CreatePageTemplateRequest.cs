namespace SimpleModule.PageBuilder.Contracts;

public class CreatePageTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
