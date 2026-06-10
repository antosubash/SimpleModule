---
outline: deep
---

# Policies

Permissions answer "can this user perform this kind of action?" (`Products.Update`). They cannot express instance-level rules like "users may only edit *their own* orders" or "tenant admins may only manage users *in their tenant*". Policies fill that gap.

A policy is a class that encapsulates every per-resource authorization rule for one entity, inspired by [Laravel's policy classes](https://laravel.com/docs/authorization#creating-policies). Policies **layer on top of** permissions — the permission stays on the endpoint as the coarse capability gate; the policy adds the instance check.

The framework ships three reference policies:

| Module | Policy | Rule |
|--------|--------|------|
| Notifications | `NotificationPolicy` | recipient-only; declarative `AuthorizeResource` form |
| FileStorage | `FileStoragePolicy` | uploader-or-admin; imperative `IAuthorizer` form |
| Users | `UserPolicy` | self-or-admin view/update, admin-only delete |

## Defining a Policy

Implement `IPolicy<TResource>` in the module that owns the resource (SM0060). The class must be effectively `public` (SM0059) and non-generic (SM0061), and the resource type must be a contracts DTO — a `[Dto]` type or a type declared in your `.Contracts` assembly (SM0058).

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

### Registration

There is no registration step. The source generator discovers every `IPolicy<T>` implementation — in implementation, contracts, and host assemblies, including nested classes — and registers it as a scoped service in the generated `AddModules()`:

```csharp
// generated
services.TryAddEnumerable(ServiceDescriptor.Scoped<IPolicy<Notification>, NotificationPolicy>());
```

`TryAddEnumerable` deduplicates **type-based** manual registrations (`AddScoped<IPolicy<X>, XPolicy>()`). If you must register via factory, use the two-generic overload — `AddScoped<IPolicy<X>, XPolicy>(sp => ...)` — so the implementation type is visible to dedup, or opt out of auto-registration entirely with `[ManualContractRegistration]` on the policy class. An opted-out policy is wired by its own module, so the auto-registration rules SM0059 (public) and SM0061 (non-generic) are waived for it; the resource rules (SM0058, SM0060) still apply.

Use the `PolicyActions` constants (`view`, `create`, `update`, `delete`) for conventional verbs, and declare module-specific actions as `public const string` on the policy class so endpoints never hardcode action strings.

## Declarative Checks: AuthorizeResource

When the handler doesn't need the resource beyond authorizing it, declare the check on the endpoint. This is what the reference module does — register an `IResourceResolver<TResource>` once per resource type:

```csharp
// modules/Notifications/.../Endpoints/Notifications/NotificationResolver.cs
internal sealed class NotificationResolver(INotificationStore store)
    : IResourceResolver<Notification>
{
    public async ValueTask<Notification?> ResolveAsync(
        string routeValue,
        CancellationToken cancellationToken = default
    ) =>
        Guid.TryParse(routeValue, out var id)
            ? await store.FindAsync(NotificationId.From(id), cancellationToken)
            : null;
}

// In ConfigureServices
services.AddScoped<IResourceResolver<Notification>, NotificationResolver>();
```

Then the endpoint declares the check instead of performing it:

```csharp
app.MapPost(
        Route,
        async Task<IResult> (Guid id, HttpContext context, INotificationsContracts notifications) =>
        {
            // AuthorizeResource already loaded the notification and ran the policy;
            // the contract call stays owner-scoped (defense in depth).
            var ok = await notifications.MarkReadAsync(
                NotificationId.From(id),
                UserId.From(context.User.GetUserId()!)
            );
            return ok ? TypedResults.NoContent() : TypedResults.NotFound();
        }
    )
    .RequirePermission(NotificationsPermissions.ViewOwn) // permission gate stays
    .AuthorizeResource<Notification>(NotificationPolicy.MarkRead); // route param "id" by default
```

The filter loads the resource (404 when missing — including when an optional route parameter is omitted), authorizes the action, and only then invokes the handler. True misconfiguration fails loudly: a route parameter name that doesn't exist in the template, or a missing resolver registration, throws `InvalidOperationException` rather than masquerading as 404.

::: warning Ordering with Form Requests
Form Request validation is attached at the route-group level and therefore runs **before** endpoint-level filters like `AuthorizeResource`. An endpoint combining both validates the caller's payload before the instance-level authorization check — harmless for security (validation only reflects the caller's own input), but the validation work happens even for requests the policy will deny. Prefer the imperative `IAuthorizer` flow if that ordering matters.
:::

## Imperative Checks: IAuthorizer

When the handler needs the loaded resource (to render it, mutate it in place, or branch on its state), inject `IAuthorizer` and follow **load → authorize → act**:

```csharp
var order = await store.FindAsync(OrderId.From(id), ct);
if (order is null)
{
    return TypedResults.NotFound();
}

// Throws on deny — translated by the global exception handler.
await authorizer.AuthorizeAsync(context.User, PolicyActions.Update, order, ct);

// ... act on the loaded order
```

To branch instead of throwing, use `CheckAsync`, which returns the `AuthorizationResult` (with `IsAllowed`, `Reason`, and `TreatAsNotFound`).

### Keep contracts owner-scoped

The policy check protects the *endpoint*, not in-process callers. Methods on your public `I{Module}Contracts` interface should stay scoped (`MarkReadAsync(id, userId)` filters by owner) so another module can never mutate or read a foreign user's data by accident. The unscoped loader the policy flow needs (`FindAsync(id)`) belongs on a **module-internal** interface — in Notifications that is `INotificationStore`.

## Denial Semantics: 403 vs 404

A denied check throws `ForbiddenException` (403) with the policy's denial reason. Reasons are returned verbatim in the response detail — write them for the end user and never include internal identifiers.

To surface a denial as 404 instead (anti-enumeration), return `AuthorizationResult.DenyAsNotFound(...)` from the policy: the policy knows whether a resource's existence is secret, and the decision travels with it. The host-level `PolicyAuthorizationOptions.NotFoundActions` set (empty by default) additionally maps every denial for the listed action names to 404 — it is a blunt host-wide override that also swallows explicit `Deny(reason)` messages, so prefer `DenyAsNotFound` and configure the option only at the host level; modules must not mutate it.

Calling `AuthorizeAsync`/`CheckAsync` for a resource type with **no registered policy throws `MissingPolicyException`** — authorization fails closed and loudly rather than silently allowing.

## Multiple Policies per Resource

More than one policy may target the same resource type — for example a tenancy-scoping policy plus an ownership policy, both owned by the resource's module (SM0060 rejects policies for other modules' resources). Policies run in registration order and evaluation stops at the first deny: **a single deny wins**, and its reason is surfaced. Allow requires every policy to allow.

## What Policies Are Not For

- **List filtering** — collection scoping belongs in queries (`WHERE UserId = @me`), not in per-item policy checks. Policies guard single instances.
- **Coarse capabilities** — "can this user use this feature at all" stays a permission string on the endpoint.
- **Other modules' entities** — a policy lives in the module that owns the resource; SM0060 enforces it for module code. Policies in assemblies that map to no module — a host assembly without its own `[Module]` class, or a shared library — are exempt, since the composition root may layer host-wide rules on any resource. Use that power deliberately: such a policy participates in deny-wins for the module's resource, and if your host declares its own `[Module]`, its policies count as that module's and SM0060 applies.
- **Replacing service-level scoping** — contract methods keep their owner filters; policies add endpoint-level semantics on top, they don't substitute for defense in depth.

## Rules Summary

| Rule | Enforced by |
|------|-------------|
| Resource type must be a contracts DTO | SM0058 (build error) |
| Policy class must be effectively public | SM0059 (build error; waived for `[ManualContractRegistration]`) |
| Policy must be owned by the resource's module | SM0060 (build error; module code only — unmapped host/shared assemblies are exempt) |
| Policy class must not be generic | SM0061 (build error; waived for `[ManualContractRegistration]`) |
| Policies auto-registered as scoped services | Source generator (`TryAddEnumerable`) |
| Missing policy at check time fails closed | `MissingPolicyException` |
| Deny wins across multiple policies | `IAuthorizer` |
| Denial as 404 | `DenyAsNotFound` per decision (preferred); `PolicyAuthorizationOptions` host override |

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
