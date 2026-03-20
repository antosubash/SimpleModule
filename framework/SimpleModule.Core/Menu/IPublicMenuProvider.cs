namespace SimpleModule.Core.Menu;

public interface IPublicMenuProvider
{
    Task<IReadOnlyList<PublicMenuItem>> GetMenuTreeAsync();
    Task<string?> GetHomePageUrlAsync();
}
