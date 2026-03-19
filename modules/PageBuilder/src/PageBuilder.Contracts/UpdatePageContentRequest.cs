using SimpleModule.Core;

namespace SimpleModule.PageBuilder.Contracts;

[Dto]
public class UpdatePageContentRequest
{
    public string Content { get; set; } = string.Empty;
}
