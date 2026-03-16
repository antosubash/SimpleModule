namespace SimpleModule.Core.Menu;

public interface IMenuRegistry
{
    IReadOnlyList<MenuItem> GetItems(MenuSection section);
}
