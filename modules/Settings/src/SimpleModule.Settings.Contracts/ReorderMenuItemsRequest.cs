namespace SimpleModule.Settings.Contracts;

public class ReorderMenuItemsRequest
{
    public List<ReorderItem> Items { get; set; } = [];
}

public class ReorderItem
{
    public PublicMenuItemId Id { get; set; }
    public PublicMenuItemId? ParentId { get; set; }
    public int SortOrder { get; set; }
}
