import { EchoProvider, type EchoStatus, useEchoStatus, useEvent } from '@simplemodule/echo';
import {
  Alert,
  AlertDescription,
  AlertTitle,
  Button,
  Card,
  CardContent,
  PageShell,
} from '@simplemodule/ui';
import { useState } from 'react';

interface Props {
  channel: string | null;
  userId: string | null;
  fireUrl: string;
}

export default function Broadcasting({ channel, userId, fireUrl }: Props) {
  return (
    <PageShell
      title="Live broadcasting"
      description="Smoke test for the SignalR hub and the @simplemodule/echo client."
    >
      {channel ? (
        <EchoProvider>
          <DemoBody channel={channel} userId={userId!} fireUrl={fireUrl} />
        </EchoProvider>
      ) : (
        <Alert variant="warning">
          <AlertTitle>Sign in required</AlertTitle>
          <AlertDescription>
            Broadcasting channels are scoped to the authenticated user.
          </AlertDescription>
        </Alert>
      )}
    </PageShell>
  );
}

function DemoBody({
  channel,
  userId,
  fireUrl,
}: {
  channel: string;
  userId: string;
  fireUrl: string;
}) {
  const status = useEchoStatus();
  const [count, setCount] = useState(0);
  const [lastAt, setLastAt] = useState<string | null>(null);
  const [firing, setFiring] = useState(false);

  useEvent<{ at: string }>(channel, 'demo.tick', (payload) => {
    setCount((c) => c + 1);
    setLastAt(payload.at);
  });

  return (
    <Card>
      <CardContent>
        <div className="space-y-4">
          <div className="flex items-center justify-between gap-3">
            <div>
              <div className="text-xs text-text-muted">Connection</div>
              <ConnectionBadge status={status} />
            </div>
            <div className="text-right">
              <div className="text-xs text-text-muted">Channel</div>
              <code className="text-sm">{channel}</code>
            </div>
          </div>

          <div className="rounded-xl border border-border p-6 text-center">
            <div className="text-xs text-text-muted">Ticks received</div>
            <div className="text-4xl font-bold tabular-nums">{count}</div>
            {lastAt && <div className="text-xs text-text-muted mt-2">last: {lastAt}</div>}
          </div>

          <Button
            disabled={firing || status !== 'connected'}
            onClick={async () => {
              setFiring(true);
              try {
                await fetch(fireUrl, { method: 'POST', credentials: 'same-origin' });
              } finally {
                setFiring(false);
              }
            }}
          >
            Fire tick for {userId}
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}

const STATUS_LABEL: Record<EchoStatus, string> = {
  disconnected: 'disconnected',
  connecting: 'connecting…',
  connected: 'connected',
  reconnecting: 'reconnecting…',
};

const STATUS_COLOR: Record<EchoStatus, string> = {
  disconnected: 'text-danger',
  connecting: 'text-warning',
  connected: 'text-success',
  reconnecting: 'text-warning',
};

function ConnectionBadge({ status }: { status: EchoStatus }) {
  return (
    <div className={`text-sm font-medium ${STATUS_COLOR[status]}`}>{STATUS_LABEL[status]}</div>
  );
}
