using SimpleModule.Core;

namespace SimpleModule.Items.Contracts;

[Dto]
public class Item
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
