using SimpleModule.Core;

namespace SimpleModule.PageBuilder.Contracts;

[Dto]
public class PageTag
{
    public PageTagId Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
