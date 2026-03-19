using SimpleModule.Core;

namespace SimpleModule.PageBuilder.Contracts;

[Dto]
public class PageSummary
{
    public PageId Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public bool HasDraft { get; set; }
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public List<string> Tags { get; set; } = [];
}
