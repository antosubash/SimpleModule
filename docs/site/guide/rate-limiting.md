---
outline: deep
---

# Rate Limiting

SimpleModule wires ASP.NET Core's built-in rate limiter into the request pipeline and lets you author policies in two places: in code, for endpoints whose limits are part of the framework contract, and in the database, for limits an administrator should tune at runtime without redeploying.

## Applying a policy to an endpoint

Use the `RateLimit(policyName)` extension on any endpoint convention builder:

```csharp
using SimpleModule.Core.RateLimiting;

public sealed class CreateEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/products", Handle)
           .RateLimit(RateLimitPolicies.FixedDefault);
}
```

The constants in `RateLimitPolicies` are the canonical names of the policies the framework ships with, so a rename can never silently miss a call site:

| Constant | Wire name | Notes |
|---|---|---|
| `FixedDefault` | `fixed-default` | Sensible default for most read/write endpoints. |
| `SlidingStrict` | `sliding-strict` | Tighter sliding window for hot paths. |
| `TokenBucket` | `token-bucket` | Bursty workloads (file uploads, batch ingestion). |
| `AuthStrict` | `auth-strict` | Applied to OpenIddict login/refresh endpoints by default. |

## DB-defined rules

Each shipped policy is backed by a row in the `RateLimiting_Rules` table. An administrator can change the limit, window, or target without a deploy — `RateLimitRuleCache` reloads the live rules and rebuilds the runtime partitioner.

A rule has these knobs (`modules/RateLimiting/src/SimpleModule.RateLimiting.Contracts/RateLimitRule.cs`):

| Field | Type | Description |
|---|---|---|
| `PolicyName` | `string` | The wire name applied via `.RateLimit("...")`. |
| `PolicyType` | `FixedWindow` \| `SlidingWindow` \| `TokenBucket` | Algorithm. |
| `Target` | `Ip` \| `User` \| `IpAndUser` \| `Global` | What to partition on. |
| `PermitLimit` | `int` | Requests allowed per window (fixed/sliding). |
| `WindowSeconds` | `int` | Window length in seconds. |
| `SegmentsPerWindow` | `int` | Sliding-window resolution. |
| `TokenLimit` | `int` | Bucket size (token bucket). |
| `TokensPerPeriod` | `int` | Refill rate (token bucket). |
| `ReplenishmentPeriodSeconds` | `int` | Refill period (token bucket). |
| `QueueLimit` | `int` | Requests parked when capacity is exhausted. |
| `EndpointPattern` | `string?` | Optional route pattern to narrow the rule to a subset of endpoints. |
| `IsEnabled` | `bool` | Toggle without deleting. |

## Admin UI

The RateLimiting module mounts an admin view at `/rate-limiting/manage` and exposes the policy CRUD over `/api/rate-limiting/`. The endpoints are protected by `RateLimiting.View`, `RateLimiting.Create`, `RateLimiting.Update`, and `RateLimiting.Delete` permissions; the active-policies preview at `/api/rate-limiting/active` is read-only.

## Querying from other modules

If a module needs to read or mutate rules programmatically, depend on the contract:

```csharp
public interface IRateLimitingContracts
{
    Task<IEnumerable<RateLimitRule>> GetAllRulesAsync();
    Task<RateLimitRule?> GetRuleByIdAsync(RateLimitRuleId id);
    Task<RateLimitRule> CreateRuleAsync(CreateRateLimitRuleRequest request);
    Task<RateLimitRule> UpdateRuleAsync(RateLimitRuleId id, UpdateRateLimitRuleRequest request);
    Task DeleteRuleAsync(RateLimitRuleId id);
}
```

## Response headers

When a policy rejects a request the framework returns `429 Too Many Requests`. The `RateLimitHeaderMiddleware` adds standard `Retry-After` and the remaining-budget headers based on the configured policy so well-behaved clients can back off without guessing.

## Next Steps

- [Configuration](/reference/configuration) — built-in policy defaults and storage tuning.
- [Endpoints](/guide/endpoints) — the `.RateLimit(...)` extension in context.
