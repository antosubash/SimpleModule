# @simplemodule/echo

Real-time client for SimpleModule's broadcasting hub (`/hub/broadcast`).
Wraps `@microsoft/signalr` and exposes React hooks for subscribing to
channels, listening to broadcast events, and observing presence
membership.

```tsx
import { EchoProvider, useEvent, usePresence } from '@simplemodule/echo';

<EchoProvider>
  <NotificationBell />
</EchoProvider>;

function NotificationBell() {
  const [count, setCount] = useState(0);
  useEvent<NotificationCreated>('private-users.123', 'notifications.created', () =>
    setCount((c) => c + 1)
  );
  return <span>{count}</span>;
}
```

The hub URL defaults to `/hub/broadcast` (same-origin); pass `url` to
`EchoProvider` for cross-origin deployments. Authentication uses the
ambient browser session cookie — no token plumbing required when the
React app is served by the SimpleModule host.
