namespace SimpleModule.Core.Menu;

public interface IPublicMenuProvider
{
    Task<string?> GetHomePageUrlAsync();
}
