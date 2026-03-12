namespace SimpleModule.Core.Menu;

public sealed class MenuBuilder : IMenuBuilder
{
    private readonly List<MenuItem> _items = [];

    public IMenuBuilder Add(MenuItem item)
    {
        _items.Add(item);
        return this;
    }

    public List<MenuItem> ToList() => _items.OrderBy(i => i.Order).ToList();
}
