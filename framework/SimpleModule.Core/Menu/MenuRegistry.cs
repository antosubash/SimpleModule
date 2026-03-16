namespace SimpleModule.Core.Menu;

public sealed class MenuRegistry : IMenuRegistry
{
    private readonly Dictionary<MenuSection, List<MenuItem>> _bySection;

    public MenuRegistry(List<MenuItem> items)
    {
        _bySection = items.GroupBy(i => i.Section).ToDictionary(g => g.Key, g => g.ToList());
    }

    public IReadOnlyList<MenuItem> GetItems(MenuSection section) =>
        _bySection.TryGetValue(section, out var items) ? items : [];
}
