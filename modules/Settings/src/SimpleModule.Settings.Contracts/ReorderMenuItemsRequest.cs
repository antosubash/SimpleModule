namespace SimpleModule.Settings.Contracts;

public class ReorderMenuItemsRequest
{
    public List<ReorderItem> Items { get; set; } = [];
}

public class ReorderItem
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public int SortOrder { get; set; }
}
