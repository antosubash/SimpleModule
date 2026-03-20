using SimpleModule.Core;

namespace SimpleModule.Settings.Contracts;

[Dto]
public class ReorderMenuItemsRequest
{
    public List<ReorderItem> Items { get; set; } = [];
}

[Dto]
public class ReorderItem
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public int SortOrder { get; set; }
}
