# Maintenance mode

Put the running app into a maintenance state during deployments, database
migrations, or any change that must not interleave with live traffic. Visitors
see a branded 503 page; the operator running the deploy can keep verifying
the release through a bypass cookie.

## Quick start

```bash
# Drop the gate. Holders of ?sm_bypass=let-me-in pass through.
sm down --secret let-me-in --message "Deploying v1.4" --retry 90

# Verify state.
sm down --status

# Lift the gate.
sm up
```

`sm down` writes a JSON sentinel at `<host-content-root>/.maintenance`. The
running app polls that path once per second; the gate engages within a
second of the file appearing and lifts within a second of `sm up`.

The secret is hashed with SHA-256 before it touches disk — only the hash
sits in the sentinel, never the plaintext.

## How the middleware behaves

`MaintenanceModeMiddleware` runs after static-asset routing and before
authentication. Once the sentinel is active:

- **Health probes** (`/health/live`, `/health/ready`) pass through unchanged
  so load balancers can still distinguish "host is down" from "deployment in
  progress".
- **Bypass query** — `?sm_bypass=<secret>` redirects with an `sm_bypass`
  cookie (HttpOnly, Secure when HTTPS, `SameSite=Lax`). The cookie's value
  is the SHA-256 hash, not the secret itself, so leaking the cookie still
  requires brute-forcing the hash to recover the secret.
- **Bypass cookie** — subsequent requests with a matching cookie pass
  through. The cookie expires after 12 hours by default.
- **Inertia / API requests** — receive a JSON 503 with `Retry-After` and
  `Cache-Control: no-store` so SPAs can show the maintenance page without
  triggering a full reload.
- **Browser requests** — receive a minimal HTML 503 with the same headers,
  styled inline so it renders without any JS bundle.

## Sentinel shape

```json
{
  "Until": "2026-05-15T18:00:00+00:00",
  "SecretHash": "f1b8c2…",
  "Message": "Deploying v1.4",
  "RetryAfterSeconds": 90
}
```

All fields are optional. An empty file (or `{}`) still engages maintenance
mode — the values just default to "no bypass possible, generic message,
retry in 60 seconds".

## Tuning

The default options live in `MaintenanceModeOptions` and are bound through
`IOptions<MaintenanceModeOptions>`:

| Option | Default | Notes |
| --- | --- | --- |
| `SentinelFileName` | `.maintenance` | Relative to the host content root. |
| `PollInterval` | 1 s | How often the middleware re-checks the file. |
| `BypassCookieName` | `sm_bypass` | Change if it collides with an app cookie. |
| `BypassCookieLifetime` | 12 h | How long a bypass stays valid. |

Override in `Program.cs`:

```csharp
builder.Services.Configure<MaintenanceModeOptions>(o =>
{
    o.PollInterval = TimeSpan.FromMilliseconds(500);
});
```

## Not in scope

- **Per-tenant maintenance** — a follow-up issue. The current sentinel is
  global to the host.
- **Distributed coordination** — each instance reads its own filesystem.
  For containerized deploys, run `sm down` against a shared volume or wire
  the sentinel into your deploy pipeline so every replica sees it.
