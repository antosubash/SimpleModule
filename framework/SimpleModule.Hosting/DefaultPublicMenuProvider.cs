using SimpleModule.Core.Menu;

namespace SimpleModule.Hosting;

/// <summary>
/// Default no-op implementation of <see cref="IPublicMenuProvider"/> used when no module
/// registers its own provider. Returns an empty menu and no home page URL.
/// </summary>
#pragma warning disable CA1812 // Instantiated via DI (TryAddScoped)
internal sealed class DefaultPublicMenuProvider : IPublicMenuProvider
#pragma warning restore CA1812
{
    public Task<IReadOnlyList<PublicMenuItem>> GetMenuTreeAsync() =>
        Task.FromResult<IReadOnlyList<PublicMenuItem>>(Array.Empty<PublicMenuItem>());

    public Task<string?> GetHomePageUrlAsync() => Task.FromResult<string?>(null);
}
