import { router } from '@inertiajs/react';
import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';
import { AdminKeys } from '@/Locales/keys';

interface Session {
  tokenId: string;
  type: string;
  applicationName: string | null;
  creationDate: string | null;
  expirationDate: string | null;
}

interface Props {
  userId: string;
  activeSessions: Session[];
  onRevokeAll: () => void;
}

export function UserSessionsTab({ userId, activeSessions, onRevokeAll }: Props) {
  const { t } = useTranslation('Admin');

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle>{t(AdminKeys.UsersEdit.ActiveSessionsTitle)}</CardTitle>
          {activeSessions.length > 0 && (
            <Button variant="danger" size="sm" onClick={onRevokeAll}>
              {t(AdminKeys.UsersEdit.RevokeAllButton)}
            </Button>
          )}
        </div>
      </CardHeader>
      <CardContent>
        {activeSessions.length === 0 ? (
          <p className="text-sm text-text-muted">{t(AdminKeys.UsersEdit.NoActiveSessions)}</p>
        ) : (
          <div className="overflow-x-auto -mx-4 px-4 sm:mx-0 sm:px-0">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t(AdminKeys.UsersEdit.ColType)}</TableHead>
                  <TableHead>{t(AdminKeys.UsersEdit.ColApplication)}</TableHead>
                  <TableHead>{t(AdminKeys.UsersEdit.ColCreated)}</TableHead>
                  <TableHead>{t(AdminKeys.UsersEdit.ColExpires)}</TableHead>
                  <TableHead />
                </TableRow>
              </TableHeader>
              <TableBody>
                {activeSessions.map((session) => (
                  <TableRow key={session.tokenId}>
                    <TableCell>
                      <Badge variant={session.type === 'refresh_token' ? 'info' : 'default'}>
                        {session.type === 'refresh_token'
                          ? t(AdminKeys.UsersEdit.SessionTypeRefresh)
                          : t(AdminKeys.UsersEdit.SessionTypeAccess)}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-sm">{session.applicationName || '\u2014'}</TableCell>
                    <TableCell className="text-sm text-text-muted">
                      {session.creationDate
                        ? new Date(session.creationDate).toLocaleString()
                        : '\u2014'}
                    </TableCell>
                    <TableCell className="text-sm text-text-muted">
                      {session.expirationDate
                        ? new Date(session.expirationDate).toLocaleString()
                        : t(AdminKeys.UsersEdit.SessionExpiresNever)}
                    </TableCell>
                    <TableCell>
                      <Button
                        variant="danger"
                        size="sm"
                        onClick={() =>
                          router.delete(`/admin/users/${userId}/sessions/${session.tokenId}`)
                        }
                      >
                        {t(AdminKeys.UsersEdit.RevokeButton)}
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
