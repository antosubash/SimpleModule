import { router } from '@inertiajs/react';
import { routes } from '@simplemodule/client/routes';
import {
  Badge,
  Button,
  Card,
  CardContent,
  PageShell,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';

interface Session {
  tokenId: string;
  type: string;
  applicationName: string | null;
  creationDate: string | null;
  expirationDate: string | null;
  isCurrent: boolean;
}

interface Props {
  sessions: Session[];
}

const STALE_THRESHOLD_MS = 30 * 24 * 60 * 60 * 1000;

export default function ActiveSessions({ sessions }: Props) {
  const hasOtherSessions = sessions.some((s) => !s.isCurrent);
  const staleBefore = Date.now() - STALE_THRESHOLD_MS;

  function handleRevoke(tokenId: string) {
    router.post(routes.openIddict.api.revokeSession(tokenId));
  }

  function handleRevokeOthers() {
    router.post(routes.openIddict.api.revokeOtherSessions());
  }

  return (
    <PageShell
      title="Active sessions"
      description="Apps and devices that are currently signed in to your account."
      breadcrumbs={[
        { label: 'Home', href: '/' },
        { label: 'Account Settings', href: '/Identity/Account/Manage' },
        { label: 'Active sessions' },
      ]}
    >
      <Card>
        <CardContent className="p-4 sm:p-6 md:p-8">
          {hasOtherSessions && (
            <div className="flex justify-end mb-4">
              <Button variant="danger" size="sm" onClick={handleRevokeOthers}>
                Sign out of all other devices
              </Button>
            </div>
          )}
          {sessions.length === 0 ? (
            <p className="text-sm text-text-muted">No active sessions.</p>
          ) : (
            <div className="overflow-x-auto -mx-4 px-4 sm:mx-0 sm:px-0">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Type</TableHead>
                    <TableHead>Application</TableHead>
                    <TableHead>Created</TableHead>
                    <TableHead>Expires</TableHead>
                    <TableHead />
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {sessions.map((session) => {
                    const created = session.creationDate
                      ? new Date(session.creationDate)
                      : null;
                    const isStale =
                      !session.isCurrent &&
                      created !== null &&
                      created.getTime() < staleBefore;
                    return (
                      <TableRow key={session.tokenId}>
                        <TableCell>
                          <div className="flex flex-wrap items-center gap-2">
                            <Badge
                              variant={session.type === 'refresh_token' ? 'info' : 'default'}
                            >
                              {session.type === 'refresh_token' ? 'Refresh' : 'Access'}
                            </Badge>
                            {session.isCurrent && <Badge variant="success">This device</Badge>}
                            {isStale && <Badge variant="warning">Stale</Badge>}
                          </div>
                        </TableCell>
                        <TableCell className="text-sm">
                          {session.applicationName || '—'}
                        </TableCell>
                        <TableCell className="text-sm text-text-muted">
                          {created ? created.toLocaleString() : '—'}
                        </TableCell>
                        <TableCell className="text-sm text-text-muted">
                          {session.expirationDate
                            ? new Date(session.expirationDate).toLocaleString()
                            : 'Never'}
                        </TableCell>
                        <TableCell>
                          {!session.isCurrent && (
                            <Button
                              variant="danger"
                              size="sm"
                              onClick={() => handleRevoke(session.tokenId)}
                            >
                              Revoke
                            </Button>
                          )}
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </div>
          )}
        </CardContent>
      </Card>
    </PageShell>
  );
}
