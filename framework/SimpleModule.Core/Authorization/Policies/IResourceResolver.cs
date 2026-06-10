namespace SimpleModule.Core.Authorization.Policies;

/// <summary>
/// Loads a resource from a route value for the declarative
/// <c>AuthorizeResource&lt;TResource&gt;</c> endpoint filter. Modules that opt into the
/// declarative form register one resolver per resource type in
/// <c>ConfigureServices</c>; return null when the resource does not exist (surfaces 404).
/// </summary>
public interface IResourceResolver<TResource>
{
    ValueTask<TResource?> ResolveAsync(
        string routeValue,
        CancellationToken cancellationToken = default
    );
}
