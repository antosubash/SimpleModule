namespace SimpleModule.Core.Menu;

public interface IPublicMenuProvider
{
    Task<IReadOnlyList<PublicMenuItem>> GetMenuTreeAsync(CancellationToken cancellationToken = default);
}
