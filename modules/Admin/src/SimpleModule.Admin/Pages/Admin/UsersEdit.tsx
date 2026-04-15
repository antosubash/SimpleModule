import { router } from '@inertiajs/react';
import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Badge,
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
  Button,
  Container,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@simplemodule/ui';
import { useState } from 'react';
import { TabNav } from '@/components/TabNav';
import { AdminKeys } from '@/Locales/keys';
import { UserDetailsTab } from './components/UserDetailsTab';
import { UserRolesTab } from './components/UserRolesTab';
import { UserSecurityTab } from './components/UserSecurityTab';
import { UserSessionsTab } from './components/UserSessionsTab';

interface UserDetail {
  id: string;
  displayName: string;
  email: string;
  emailConfirmed: boolean;
  twoFactorEnabled: boolean;
  roles: string[];
  isLockedOut: boolean;
  isDeactivated: boolean;
  accessFailedCount: number;
  createdAt: string;
  lastLoginAt: string | null;
}

interface Role {
  id: string;
  name: string;
  description: string | null;
}

interface Session {
  tokenId: string;
  type: string;
  applicationName: string | null;
  creationDate: string | null;
  expirationDate: string | null;
}

interface Props {
  user: UserDetail;
  userPermissions: string[];
  allRoles: Role[];
  permissionsByModule: Record<string, string[]>;
  activeSessions: Session[];
  tab: string;
  currentUserId: string;
}

type ConfirmAction = 'deactivate' | 'reverify' | 'disable2fa' | 'revokeAll' | null;

export default function UsersEdit({
  user,
  userPermissions,
  allRoles,
  permissionsByModule,
  activeSessions,
  tab,
  currentUserId,
}: Props) {
  const { t } = useTranslation('Admin');
  const [confirmAction, setConfirmAction] = useState<ConfirmAction>(null);

  const isSelf = user.id === currentUserId;

  const tabs = [
    { id: 'details', label: t(AdminKeys.UsersEdit.TabDetails) },
    { id: 'roles', label: t(AdminKeys.UsersEdit.TabRoles) },
    { id: 'security', label: t(AdminKeys.UsersEdit.TabSecurity) },
    { id: 'sessions', label: t(AdminKeys.UsersEdit.TabSessions) },
  ];

  function handleConfirmAction() {
    switch (confirmAction) {
      case 'deactivate':
        router.post(`/admin/users/${user.id}/deactivate`);
        break;
      case 'reverify':
        router.post(`/admin/users/${user.id}/force-reverify`);
        break;
      case 'disable2fa':
        router.post(`/admin/users/${user.id}/disable-2fa`);
        break;
      case 'revokeAll':
        router.delete(`/admin/users/${user.id}/sessions`);
        break;
    }
    setConfirmAction(null);
  }

  const confirmDialogConfig: Record<
    Exclude<ConfirmAction, null>,
    { title: string; description: string; action: string }
  > = {
    deactivate: {
      title: t(AdminKeys.UsersEdit.ConfirmDeactivateTitle),
      description: t(AdminKeys.UsersEdit.ConfirmDeactivateDescription),
      action: t(AdminKeys.UsersEdit.ConfirmDeactivateAction),
    },
    reverify: {
      title: t(AdminKeys.UsersEdit.ConfirmReverifyTitle),
      description: t(AdminKeys.UsersEdit.ConfirmReverifyDescription),
      action: t(AdminKeys.UsersEdit.ConfirmReverifyAction),
    },
    disable2fa: {
      title: t(AdminKeys.UsersEdit.ConfirmDisable2faTitle),
      description: t(AdminKeys.UsersEdit.ConfirmDisable2faDescription),
      action: t(AdminKeys.UsersEdit.ConfirmDisable2faAction),
    },
    revokeAll: {
      title: t(AdminKeys.UsersEdit.ConfirmRevokeAllTitle),
      description: t(AdminKeys.UsersEdit.ConfirmRevokeAllDescription),
      action: t(AdminKeys.UsersEdit.ConfirmRevokeAllAction),
    },
  };

  const dialogConfig = confirmAction ? confirmDialogConfig[confirmAction] : null;

  return (
    <Container className="space-y-4 sm:space-y-6">
      <Breadcrumb>
        <BreadcrumbList>
          <BreadcrumbItem>
            <BreadcrumbLink href="/admin/users">
              {t(AdminKeys.UsersEdit.BreadcrumbUsers)}
            </BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbPage>{t(AdminKeys.UsersEdit.BreadcrumbEdit)}</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>
      <div className="flex flex-wrap items-center gap-2 sm:gap-3">
        <h1 className="text-2xl font-bold tracking-tight">{t(AdminKeys.UsersEdit.Title)}</h1>
        {isSelf && <Badge variant="info">{t(AdminKeys.UsersEdit.BadgeYou)}</Badge>}
        {user.isDeactivated && (
          <Badge variant="default">{t(AdminKeys.UsersEdit.BadgeDeactivated)}</Badge>
        )}
        {user.isLockedOut && !user.isDeactivated && (
          <Badge variant="danger">{t(AdminKeys.UsersEdit.BadgeLocked)}</Badge>
        )}
      </div>

      <TabNav tabs={tabs} activeTab={tab} baseUrl={`/admin/users/${user.id}/edit`} />

      {tab === 'details' && (
        <UserDetailsTab
          user={user}
          isSelf={isSelf}
          onDeactivate={() => setConfirmAction('deactivate')}
        />
      )}

      {tab === 'roles' && (
        <UserRolesTab
          userId={user.id}
          userRoles={user.roles}
          allRoles={allRoles}
          userPermissions={userPermissions}
          permissionsByModule={permissionsByModule}
        />
      )}

      {tab === 'security' && (
        <UserSecurityTab
          user={user}
          isSelf={isSelf}
          onReverify={() => setConfirmAction('reverify')}
          onDisable2fa={() => setConfirmAction('disable2fa')}
        />
      )}

      {tab === 'sessions' && (
        <UserSessionsTab
          userId={user.id}
          activeSessions={activeSessions}
          onRevokeAll={() => setConfirmAction('revokeAll')}
        />
      )}

      <Dialog
        open={confirmAction !== null}
        onOpenChange={(open) => !open && setConfirmAction(null)}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{dialogConfig?.title}</DialogTitle>
            <DialogDescription>{dialogConfig?.description}</DialogDescription>
          </DialogHeader>
          <DialogFooter className="flex flex-wrap gap-2">
            <Button variant="secondary" onClick={() => setConfirmAction(null)}>
              {t(AdminKeys.UsersEdit.CancelButton)}
            </Button>
            <Button variant="danger" onClick={handleConfirmAction}>
              {dialogConfig?.action}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </Container>
  );
}
