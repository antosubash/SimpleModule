import { router } from '@inertiajs/react';
import { routes } from '@simplemodule/client/routes';
import {
  Badge,
  Button,
  EmptyState,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';
import ManageLayout from '@/components/ManageLayout';

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
    <ManageLayout activePage="ActiveSessions">
      <div className="space-y-4">
        <div className="flex items-start justify-between gap-4">
          <div>
            <h2 className="text-lg font-medium">Active sessions</h2>
            <p className="text-sm text-text-muted">
              Apps and devices that are currently signed in to your account.
            </p>
          </div>
          {hasOtherSessions && (
            <Button variant="danger" size="sm" onClick={handleRevokeOthers}>
              Sign out of all other devices
            </Button>
          )}
        </div>
        {sessions.length === 0 ? (
          <EmptyState
            title="No active sessions."
            description="Apps and devices that are currently signed in to your account will appear here."
          />
        ) : (
          <div className="overflow-x-auto -mx-4 px-4 sm:mx-0 sm:px-0">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Status</TableHead>
                  <TableHead>Application</TableHead>
                  <TableHead>Created</TableHead>
                  <TableHead>Expires</TableHead>
                  <TableHead />
                </TableRow>
              </TableHeader>
              <TableBody>
                {sessions.map((session) => {
                  const created = session.creationDate ? new Date(session.creationDate) : null;
                  const isStale =
                    !session.isCurrent && created !== null && created.getTime() < staleBefore;
                  return (
                    <TableRow key={session.tokenId}>
                      <TableCell>
                        <div className="flex flex-wrap items-center gap-2">
                          {session.isCurrent ? (
                            <Badge variant="success">This device</Badge>
                          ) : (
                            <Badge variant="default">Active</Badge>
                          )}
                          {isStale && <Badge variant="warning">Stale</Badge>}
                        </div>
                      </TableCell>
                      <TableCell className="text-sm">{session.applicationName || '—'}</TableCell>
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
      </div>
    </ManageLayout>
  );
}
