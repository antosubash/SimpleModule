using SimpleModule.Core;

namespace SimpleModule.PageBuilder.Contracts;

[Dto]
public class CreatePageTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
