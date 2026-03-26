namespace SimpleModule.PageBuilder.Contracts;

public class CreatePageRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Slug { get; set; }
}
