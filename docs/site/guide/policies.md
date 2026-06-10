---
outline: deep
---

# Policies

Permissions answer "can this user perform this kind of action?" (`Products.Update`). They cannot express instance-level rules like "users may only edit *their own* orders" or "tenant admins may only manage users *in their tenant*". Policies fill that gap.

A policy is a class that encapsulates every per-resource authorization rule for one entity, inspired by [Laravel's policy classes](https://laravel.com/docs/authorization#creating-policies). Policies **layer on top of** permissions — the permission stays on the endpoint as the coarse capability gate; the policy adds the instance check.

## Defining a Policy

Implement `IPolicy<TResource>` in the module that owns the resource. The resource type must be a contracts DTO — a `[Dto]` type or a public type in your `.Contracts` assembly (enforced by diagnostic SM0058).

```csharp
using System.Security.Claims;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Authorization.Policies;
using SimpleModule.Core.Extensions;
using SimpleModule.Notifications.Contracts;
using SimpleModule.Users.Contracts;

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
            PolicyActions.View or MarkRead => AllowOwnerOrAdmin(user, resource),
            _ => AuthorizationResult.Deny($"Unknown notification action '{action}'."),
        };
        return Task.FromResult(result);
    }

    private static AuthorizationResult AllowOwnerOrAdmin(
        ClaimsPrincipal user,
        Notification notification
    )
    {
        if (user.IsInRole(WellKnownRoles.Admin))
        {
            return AuthorizationResult.Allow();
        }

        var userId = user.GetUserId();
        return userId is not null && notification.UserId == UserId.From(userId)
            ? AuthorizationResult.Allow()
            : AuthorizationResult.Deny("You can only access your own notifications.");
    }
}
```

There is no registration step. The source generator discovers every `IPolicy<T>` implementation in module assemblies and registers it as a scoped service in the generated `AddModules()`:

```csharp
// generated
services.AddScoped<IPolicy<Notification>, NotificationPolicy>();
```

Use the `PolicyActions` constants (`view`, `create`, `update`, `delete`) for conventional verbs, and declare module-specific actions as `public const string` on the policy class so endpoints never hardcode action strings.

## Checking a Policy in an Endpoint

Inject `IAuthorizer` and follow **load → authorize → act**:

```csharp
public class MarkReadEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                Route,
                async Task<IResult> (
                    Guid id,
                    HttpContext context,
                    INotificationsContracts notifications,
                    IAuthorizer authorizer
                ) =>
                {
                    var notification = await notifications.FindAsync(NotificationId.From(id));
                    if (notification is null)
                    {
                        return TypedResults.NotFound();
                    }

                    // Throws on deny — translated by the global exception handler
                    await authorizer.AuthorizeAsync(
                        context.User,
                        NotificationPolicy.MarkRead,
                        notification,
                        context.RequestAborted
                    );

                    await notifications.MarkReadAsync(notification.Id);
                    return TypedResults.NoContent();
                }
            )
            .RequirePermission(NotificationsPermissions.ViewOwn); // permission gate stays
}
```

`AuthorizeAsync` throws on denial, so the happy path stays free of authorization if-statements. To branch instead of throwing, use `CheckAsync`, which returns the `AuthorizationResult`:

```csharp
var result = await authorizer.CheckAsync(user, PolicyActions.Update, order);
if (!result.IsAllowed)
{
    // result.Reason carries the policy's denial message
}
```

## Denial Semantics: 403 vs 404

A denied check throws `ForbiddenException` (403) with the policy's denial reason. For the `view` action it throws `NotFoundException` (404) instead, so unauthorized callers cannot probe which resource IDs exist.

The action set that maps to 404 is configurable via `PolicyAuthorizationOptions`:

```csharp
// In a module's ConfigureServices — markRead denials also surface as 404
services.Configure<PolicyAuthorizationOptions>(o =>
    o.NotFoundActions.Add(NotificationPolicy.MarkRead)
);
```

Calling `AuthorizeAsync`/`CheckAsync` for a resource type with **no registered policy throws `MissingPolicyException`** — authorization fails closed and loudly rather than silently allowing.

## Multiple Policies per Resource

More than one policy may target the same resource type — for example a tenancy-scoping policy plus an ownership policy, both owned by the resource's module. Policies run in registration order and evaluation stops at the first deny: **a single deny wins**, and its reason is surfaced. Allow requires every policy to allow.

Denial reasons are returned verbatim in the 403 response detail — write them for the end user and never include internal identifiers.

## Declarative Form

When the endpoint does nothing with the resource except authorize it, skip the manual load with `AuthorizeResource`. Register an `IResourceResolver<TResource>` once per resource type:

```csharp
// In the module
public sealed class NotificationResolver(INotificationsContracts notifications)
    : IResourceResolver<Notification>
{
    public async ValueTask<Notification?> ResolveAsync(
        string routeValue,
        CancellationToken cancellationToken = default
    ) =>
        Guid.TryParse(routeValue, out var id)
            ? await notifications.FindAsync(NotificationId.From(id))
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

The filter loads the resource (404 when missing), authorizes the action, and only then invokes the handler.

## What Policies Are Not For

- **List filtering** — collection scoping belongs in queries (`WHERE UserId = @me`), not in per-item policy checks. Policies guard single instances.
- **Coarse capabilities** — "can this user use this feature at all" stays a permission string on the endpoint.
- **Other modules' entities** — a policy lives in the module that owns the resource. Never author a policy for an entity you don't own.

## Rules Summary

| Rule | Enforced by |
|------|-------------|
| Resource type must be a contracts DTO | SM0058 (build error) |
| Policies auto-registered as scoped services | Source generator |
| Missing policy at check time fails closed | `MissingPolicyException` |
| Deny wins across multiple policies | `IAuthorizer` |
| `view` denial surfaces as 404 (configurable) | `PolicyAuthorizationOptions` |

## Testing Policies

Policies are plain classes — unit test them directly without any host:

```csharp
[Fact]
public async Task NonOwner_IsDenied()
{
    var policy = new NotificationPolicy();
    var user = new ClaimsPrincipal(
        new ClaimsIdentity([new Claim("sub", "user-2")], "test")
    );
    var notification = new Notification { UserId = UserId.From("user-1") };

    var result = await policy.AuthorizeAsync(user, PolicyActions.View, notification);

    result.IsAllowed.Should().BeFalse();
}
```
