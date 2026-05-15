# Identity throttling

Two protections for the email / phone confirmation flows:

1. **Resend cooldown** — per-user, per-channel. Stops a signed-in user from
   pumping verification emails or SMS to arbitrary addresses, which can
   drive up provider costs.
2. **Attempt cap on code submission** — per-user, per-channel. Limits how
   many bad 6-digit SMS codes (or expired email links) the user can
   present before the channel is locked out for a cooling-off window.

Both are stored via `IFusionCache` (the framework's unified cache), so
when a distributed cache backend is wired in they automatically span
process replicas without code changes.

## Configuration

```jsonc
// appsettings.json
{
  "Identity": {
    "VerificationThrottle": {
      "ResendCooldown": "00:01:00",       // 60 seconds between resends
      "MaxFailedAttempts": 5,             // 5 wrong codes…
      "LockoutDuration": "00:15:00"       // …locks out for 15 minutes
    }
  }
}
```

All three are independent — increasing `LockoutDuration` does not stretch
the resend cooldown, and vice versa.

## How it shows up

### Resend over cooldown

`POST /Identity/Account/ResendEmailConfirmation`, `/Identity/Account/Manage/Email`,
or `/Identity/Account/Manage/SendPhoneVerificationCode` while the cooldown
is active:

- Returns the same Inertia page the success path renders (no information
  leak about which addresses are registered for the anonymous resend endpoint).
- Sets a `Retry-After` header so well-behaved clients can back off.
- The status message on the manage pages tells the user how long to wait.

### Lockout on code submission

`POST /Identity/Account/Manage/ConfirmPhoneNumber` after `MaxFailedAttempts`
incorrect codes:

- Returns the manage page with a generic *"Too many failed attempts. Please
  try again later."* message.
- Subsequent attempts short-circuit at the lockout check — `ChangePhoneNumberAsync`
  is never called, so a brute-forcer can't keep burning the 6-digit search
  space against the lockout.
- A successful confirmation clears the counter and the lockout.

## Auditing

Both `Identity` endpoints run inside the existing `AuditMiddleware`, so
every resend POST and every confirmation POST already appears in the
`AuditLogs` stream with method, path, user, IP, status, and timing. The
throttle decisions are visible from the `Retry-After` header and the
distinct status messages — no separate audit-event channel needed.

## Programmatic surface

```csharp
public interface IVerificationThrottle
{
    Task<ResendDecision> TryAcquireResendSlotAsync(string userKey, VerificationChannel channel, CancellationToken ct);
    Task<VerificationAttemptDecision> RecordAttemptAsync(string userKey, VerificationChannel channel, bool succeeded, CancellationToken ct);
    Task<bool> IsLockedOutAsync(string userKey, VerificationChannel channel, CancellationToken ct);
}
```

`userKey` is whatever string you want to scope on — in the built-in
endpoints we use `UserManager.GetUserIdAsync(user)`. For anonymous flows
you could substitute a hashed IP or the requested email.

## Trade-offs

- **Why not a route-level rate limit?** SimpleModule already has
  `RateLimiting` middleware. Route-level limits would block obvious abuse,
  but a single signed-in session pacing requests under the route's
  threshold would still succeed at draining your SMS budget. The per-user
  counter is what closes that hole. Use both together for defense in depth.
- **Why FusionCache instead of `IDistributedCache` directly?** The rest of
  the framework already standardizes on FusionCache; introducing a second
  caching abstraction here would fragment ops.
- **Why a singleton service?** State lives in the cache, not the service.
  Singleton lifetime sidesteps the scoped/transient capture footguns and
  makes ctor cost negligible.
