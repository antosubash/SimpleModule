using SimpleModule.Core;

namespace SimpleModule.PageBuilder.Contracts;

[Dto]
public class CreatePageRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Slug { get; set; }
}
