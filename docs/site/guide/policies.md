---
outline: deep
---

# Policies

Permissions answer "can this user perform this kind of action?" (`Products.Update`). They cannot express instance-level rules like "users may only edit *their own* orders" or "tenant admins may only manage users *in their tenant*". Policies fill that gap.

A policy is a class that encapsulates every per-resource authorization rule for one entity, inspired by [Laravel's policy classes](https://laravel.com/docs/authorization#creating-policies). Policies **layer on top of** permissions — the permission stays on the endpoint as the coarse capability gate; the policy adds the instance check.

## Defining a Policy

Implement `IPolicy<TResource>` in the module that owns the resource (SM0060). The class must be `public` (SM0059), and the resource type must be a contracts DTO — a `[Dto]` type or a type declared in your `.Contracts` assembly (SM0058).

```csharp
using System.Security.Claims;
using SimpleModule.Core.Authorization.Policies;
using SimpleModule.Core.Extensions;

namespace SimpleModule.Notifications;

public sealed class NotificationPolicy : IPolicy<Notification>
{
    // Module-specific action beyond the conventional CRUD verbs
    public const string MarkRead = "markRead";

    public Task<AuthorizationResult> AuthorizeAsync(
        ClaimsPrincipal user,
        string action,
        Notification resource,
        CancellationToken cancellationToken = default
    )
    {
        var result = action switch
        {
            PolicyActions.View or MarkRead => AllowOwner(user, resource),
            _ => AuthorizationResult.Deny($"Unknown notification action '{action}'."),
        };
        return Task.FromResult(result);
    }

    private static AuthorizationResult AllowOwner(ClaimsPrincipal user, Notification notification)
    {
        var userId = user.GetUserId();
        return userId is not null && notification.UserId == UserId.From(userId)
            ? AuthorizationResult.Allow()
            : AuthorizationResult.DenyAsNotFound("You can only access your own notifications.");
    }
}
```

Two things worth noting in this example:

- **Admins are not exempt.** Permission checks bypass for the Admin role; policies do not. If an admin should pass an instance rule, the policy must say so explicitly — here it deliberately doesn't, because marking read mutates the recipient's inbox state.
- **`DenyAsNotFound`** makes the denial surface as 404 instead of 403, so callers cannot probe which notification IDs exist.

There is no registration step. The source generator discovers every `IPolicy<T>` implementation — in implementation and contracts assemblies, including nested classes — and registers it as a scoped service in the generated `AddModules()`, deduplicated against any manual registration:

```csharp
// generated
services.TryAddEnumerable(ServiceDescriptor.Scoped<IPolicy<Notification>, NotificationPolicy>());
```

Use the `PolicyActions` constants (`view`, `create`, `update`, `delete`) for conventional verbs, and declare module-specific actions as `public const string` on the policy class so endpoints never hardcode action strings.

## Checking a Policy in an Endpoint

Inject `IAuthorizer` and follow **load → authorize → act**:

```csharp
app.MapPost(
        Route,
        async Task<IResult> (
            Guid id,
            HttpContext context,
            INotificationStore store,             // module-internal unscoped loader
            INotificationsContracts notifications, // public, owner-scoped contract
            IAuthorizer authorizer
        ) =>
        {
            // Load: the unscoped read exists only for this flow and is module-internal.
            var notification = await store.FindAsync(NotificationId.From(id));
            if (notification is null)
            {
                return TypedResults.NotFound();
            }

            // Authorize: throws on deny — translated by the global exception handler.
            await authorizer.AuthorizeAsync(
                context.User,
                NotificationPolicy.MarkRead,
                notification,
                context.RequestAborted
            );

            // Act: the contract call stays owner-scoped (defense in depth).
            var ok = await notifications.MarkReadAsync(
                notification.Id,
                UserId.From(context.User.GetUserId()!)
            );
            return ok ? TypedResults.NoContent() : TypedResults.NotFound();
        }
    )
    .RequirePermission(NotificationsPermissions.ViewOwn); // permission gate stays
```

`AuthorizeAsync` throws on denial, so the happy path stays free of authorization if-statements. To branch instead of throwing, use `CheckAsync`, which returns the `AuthorizationResult`.

### Keep contracts owner-scoped

The policy check protects the *endpoint*, not in-process callers. Methods on your public `I{Module}Contracts` interface should stay scoped (`MarkReadAsync(id, userId)` filters by owner) so another module can never mutate or read a foreign user's data by accident. The unscoped loader the policy flow needs (`FindAsync(id)`) belongs on a **module-internal** interface — in Notifications that is `INotificationStore`.

## Denial Semantics: 403 vs 404

A denied check throws `ForbiddenException` (403) with the policy's denial reason. Reasons are returned verbatim in the response detail — write them for the end user and never include internal identifiers.

Two ways to surface a denial as 404 instead (anti-enumeration):

1. **Per decision (preferred):** return `AuthorizationResult.DenyAsNotFound(...)` from the policy. The policy knows whether a resource's existence is secret; the decision travels with it.
2. **Per action, host-wide:** `PolicyAuthorizationOptions.NotFoundActions` lists actions whose denials always map to 404 — `view` by default. This is host-level configuration; modules must not mutate it, because action names are not namespaced and a module-added entry would change semantics for every other module in the host.

Calling `AuthorizeAsync`/`CheckAsync` for a resource type with **no registered policy throws `MissingPolicyException`** — authorization fails closed and loudly rather than silently allowing.

## Multiple Policies per Resource

More than one policy may target the same resource type — for example a tenancy-scoping policy plus an ownership policy, both owned by the resource's module (SM0060 rejects policies for other modules' resources). Policies run in registration order and evaluation stops at the first deny: **a single deny wins**, and its reason is surfaced. Allow requires every policy to allow.

## Declarative Form

When the endpoint does nothing with the resource except authorize it, skip the manual load with `AuthorizeResource`. Register an `IResourceResolver<TResource>` once per resource type:

```csharp
// In the module
public sealed class NotificationResolver(INotificationStore store)
    : IResourceResolver<Notification>
{
    public async ValueTask<Notification?> ResolveAsync(
        string routeValue,
        CancellationToken cancellationToken = default
    ) =>
        Guid.TryParse(routeValue, out var id)
            ? await store.FindAsync(NotificationId.From(id))
            : null;
}

// In ConfigureServices
services.AddScoped<IResourceResolver<Notification>, NotificationResolver>();
```

Then the endpoint declares the check instead of performing it:

```csharp
app.MapPost(Route, handler)
    .RequirePermission(NotificationsPermissions.ViewOwn)
    .AuthorizeResource<Notification>(NotificationPolicy.MarkRead); // route param "id" by default
```

The filter loads the resource (404 when missing), authorizes the action, and only then invokes the handler. Misconfiguration fails loudly: a route template without the named parameter, or a missing resolver registration, throws `InvalidOperationException` rather than masquerading as 404.

## What Policies Are Not For

- **List filtering** — collection scoping belongs in queries (`WHERE UserId = @me`), not in per-item policy checks. Policies guard single instances.
- **Coarse capabilities** — "can this user use this feature at all" stays a permission string on the endpoint.
- **Other modules' entities** — a policy lives in the module that owns the resource; SM0060 enforces it.
- **Replacing service-level scoping** — contract methods keep their owner filters; policies add endpoint-level semantics on top, they don't substitute for defense in depth.

## Rules Summary

| Rule | Enforced by |
|------|-------------|
| Resource type must be a contracts DTO | SM0058 (build error) |
| Policy class must be public | SM0059 (build error) |
| Policy must be owned by the resource's module | SM0060 (build error) |
| Policies auto-registered as scoped services (dedup vs manual) | Source generator (`TryAddEnumerable`) |
| Missing policy at check time fails closed | `MissingPolicyException` |
| Deny wins across multiple policies | `IAuthorizer` |
| Denial as 404 | `DenyAsNotFound` per decision, or `PolicyAuthorizationOptions` per action (default: `view`) |

## Testing Policies

Policies are plain classes — unit test them directly without any host:

```csharp
[Fact]
public async Task NonOwner_IsDeniedAsNotFound()
{
    var policy = new NotificationPolicy();
    var user = new ClaimsPrincipal(
        new ClaimsIdentity([new Claim("sub", "user-2")], "test")
    );
    var notification = new Notification { UserId = UserId.From("user-1") };

    var result = await policy.AuthorizeAsync(user, PolicyActions.View, notification);

    result.IsAllowed.Should().BeFalse();
    result.TreatAsNotFound.Should().BeTrue();
}
```
