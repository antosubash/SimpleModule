using SimpleModule.Core;

namespace SimpleModule.PageBuilder.Contracts;

[Dto]
public class AddTagRequest
{
    public string Name { get; set; } = string.Empty;
}
