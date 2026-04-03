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
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Checkbox,
  Container,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Field,
  FieldGroup,
  Input,
  Label,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';
import { useState } from 'react';
import { AdminKeys } from '../../Locales/keys';
import { PermissionGroups } from '../components/PermissionGroups';
import { TabNav } from '../components/TabNav';

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
  const [passwordError, setPasswordError] = useState<string | null>(null);

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
        <>
          <Card>
            <CardHeader>
              <CardTitle>{t(AdminKeys.UsersEdit.DetailsTitle)}</CardTitle>
            </CardHeader>
            <CardContent>
              <form
                onSubmit={(e) => {
                  e.preventDefault();
                  router.post(`/admin/users/${user.id}`, new FormData(e.currentTarget));
                }}
              >
                <FieldGroup>
                  <Field>
                    <Label htmlFor="displayName">{t(AdminKeys.UsersEdit.FieldDisplayName)}</Label>
                    <Input id="displayName" name="displayName" defaultValue={user.displayName} />
                  </Field>
                  <Field>
                    <Label htmlFor="email">{t(AdminKeys.UsersEdit.FieldEmail)}</Label>
                    <Input id="email" name="email" type="email" defaultValue={user.email} />
                  </Field>
                  <Field orientation="horizontal">
                    <Checkbox
                      id="emailConfirmed"
                      name="emailConfirmed"
                      value="true"
                      defaultChecked={user.emailConfirmed}
                    />
                    <Label htmlFor="emailConfirmed" className="mb-0">
                      {t(AdminKeys.UsersEdit.FieldEmailConfirmed)}
                    </Label>
                  </Field>
                  <Button type="submit">{t(AdminKeys.UsersEdit.SaveDetailsButton)}</Button>
                </FieldGroup>
              </form>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>{t(AdminKeys.UsersEdit.AccountStatusTitle)}</CardTitle>
            </CardHeader>
            <CardContent>
              {user.isDeactivated ? (
                <div>
                  <p className="text-sm text-text-muted mb-3">
                    {t(AdminKeys.UsersEdit.AccountDeactivatedMessage)}
                  </p>
                  <Button
                    variant="outline"
                    onClick={() => router.post(`/admin/users/${user.id}/reactivate`)}
                  >
                    {t(AdminKeys.UsersEdit.ReactivateButton)}
                  </Button>
                </div>
              ) : isSelf ? (
                <p className="text-sm text-text-muted">
                  {t(AdminKeys.UsersEdit.CannotDeactivateSelf)}
                </p>
              ) : (
                <div>
                  <p className="text-sm text-text-muted mb-3">
                    {t(AdminKeys.UsersEdit.DeactivateWarning)}
                  </p>
                  <Button variant="danger" onClick={() => setConfirmAction('deactivate')}>
                    {t(AdminKeys.UsersEdit.DeactivateButton)}
                  </Button>
                </div>
              )}
            </CardContent>
          </Card>
        </>
      )}

      {tab === 'roles' && (
        <>
          <Card>
            <CardHeader>
              <CardTitle>{t(AdminKeys.UsersEdit.RolesTitle)}</CardTitle>
            </CardHeader>
            <CardContent>
              <form
                onSubmit={(e) => {
                  e.preventDefault();
                  router.post(`/admin/users/${user.id}/roles`, new FormData(e.currentTarget));
                }}
              >
                <div className="space-y-2 mb-4">
                  {allRoles.map((role) => (
                    <div key={role.id} className="flex items-center gap-2">
                      <Checkbox
                        id={`role-${role.id}`}
                        name="roles"
                        value={role.name ?? ''}
                        defaultChecked={user.roles.includes(role.name ?? '')}
                      />
                      <Label htmlFor={`role-${role.id}`} className="mb-0">
                        {role.name}
                        {role.description && (
                          <span className="text-text-muted ml-1">&mdash; {role.description}</span>
                        )}
                      </Label>
                    </div>
                  ))}
                  {allRoles.length === 0 && (
                    <p className="text-sm text-text-muted">
                      {t(AdminKeys.UsersEdit.NoRolesDefined)}
                    </p>
                  )}
                </div>
                <Button type="submit">{t(AdminKeys.UsersEdit.SaveRolesButton)}</Button>
              </form>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>{t(AdminKeys.UsersEdit.DirectPermissionsTitle)}</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-sm text-text-muted mb-4">
                {t(AdminKeys.UsersEdit.DirectPermissionsDescription)}
              </p>
              <form
                onSubmit={(e) => {
                  e.preventDefault();
                  router.post(`/admin/users/${user.id}/permissions`, new FormData(e.currentTarget));
                }}
              >
                <PermissionGroups
                  permissionsByModule={permissionsByModule}
                  selected={userPermissions}
                  namePrefix="permissions"
                />
                <Button type="submit" className="mt-4">
                  {t(AdminKeys.UsersEdit.SavePermissionsButton)}
                </Button>
              </form>
            </CardContent>
          </Card>
        </>
      )}

      {tab === 'security' && (
        <>
          <Card>
            <CardHeader>
              <CardTitle>{t(AdminKeys.UsersEdit.ResetPasswordTitle)}</CardTitle>
            </CardHeader>
            <CardContent>
              <form
                onSubmit={(e) => {
                  e.preventDefault();
                  const formData = new FormData(e.currentTarget);
                  if (formData.get('newPassword') !== formData.get('confirmPassword')) {
                    setPasswordError(t(AdminKeys.UsersEdit.ErrorPasswordMismatch));
                    return;
                  }
                  setPasswordError(null);
                  router.post(`/admin/users/${user.id}/reset-password`, formData);
                }}
              >
                <FieldGroup>
                  {passwordError && (
                    <div className="rounded-lg border border-danger/30 bg-danger/10 px-4 py-3 text-sm text-danger">
                      {passwordError}
                    </div>
                  )}
                  <Field>
                    <Label htmlFor="newPassword">{t(AdminKeys.UsersEdit.FieldNewPassword)}</Label>
                    <Input id="newPassword" name="newPassword" type="password" required />
                  </Field>
                  <Field>
                    <Label htmlFor="confirmPassword">
                      {t(AdminKeys.UsersEdit.FieldConfirmPassword)}
                    </Label>
                    <Input id="confirmPassword" name="confirmPassword" type="password" required />
                  </Field>
                  <Button type="submit">{t(AdminKeys.UsersEdit.ResetPasswordButton)}</Button>
                </FieldGroup>
              </form>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>{t(AdminKeys.UsersEdit.AccountLockTitle)}</CardTitle>
            </CardHeader>
            <CardContent>
              {user.isLockedOut ? (
                <div>
                  <p className="text-sm text-danger mb-3">
                    {t(AdminKeys.UsersEdit.AccountLockedMessage)}
                  </p>
                  <Button
                    variant="outline"
                    onClick={() => router.post(`/admin/users/${user.id}/unlock`)}
                  >
                    {t(AdminKeys.UsersEdit.UnlockButton)}
                  </Button>
                </div>
              ) : isSelf ? (
                <p className="text-sm text-text-muted">{t(AdminKeys.UsersEdit.CannotLockSelf)}</p>
              ) : (
                <div>
                  <p className="text-sm text-success mb-3">
                    {t(AdminKeys.UsersEdit.AccountActiveMessage)}
                  </p>
                  <Button
                    variant="danger"
                    onClick={() => router.post(`/admin/users/${user.id}/lock`)}
                  >
                    {t(AdminKeys.UsersEdit.LockButton)}
                  </Button>
                </div>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>{t(AdminKeys.UsersEdit.EmailVerificationTitle)}</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-sm text-text-muted mb-3">
                {t(AdminKeys.UsersEdit.EmailVerificationStatus, {
                  status: user.emailConfirmed
                    ? t(AdminKeys.UsersEdit.EmailVerified)
                    : t(AdminKeys.UsersEdit.EmailNotVerified),
                })}
              </p>
              {user.emailConfirmed && (
                <Button variant="outline" onClick={() => setConfirmAction('reverify')}>
                  {t(AdminKeys.UsersEdit.ForceReverifyButton)}
                </Button>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>{t(AdminKeys.UsersEdit.TwoFactorTitle)}</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-sm text-text-muted mb-3">
                {t(AdminKeys.UsersEdit.TwoFactorStatus, {
                  status: user.twoFactorEnabled
                    ? t(AdminKeys.UsersEdit.TwoFactorEnabled)
                    : t(AdminKeys.UsersEdit.TwoFactorNotEnabled),
                })}
              </p>
              {user.twoFactorEnabled && (
                <Button variant="danger" onClick={() => setConfirmAction('disable2fa')}>
                  {t(AdminKeys.UsersEdit.Disable2faButton)}
                </Button>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>{t(AdminKeys.UsersEdit.LoginInfoTitle)}</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 text-sm">
                <div>
                  <span className="text-text-muted">
                    {t(AdminKeys.UsersEdit.FailedLoginAttempts)}
                  </span>
                  <span className="ml-2 font-medium">{user.accessFailedCount}</span>
                </div>
                <div>
                  <span className="text-text-muted">{t(AdminKeys.UsersEdit.LastLogin)}</span>
                  <span className="ml-2 font-medium">
                    {user.lastLoginAt
                      ? new Date(user.lastLoginAt).toLocaleString()
                      : t(AdminKeys.UsersEdit.LastLoginNever)}
                  </span>
                </div>
                <div>
                  <span className="text-text-muted">{t(AdminKeys.UsersEdit.CreatedAt)}</span>
                  <span className="ml-2 font-medium">
                    {new Date(user.createdAt).toLocaleString()}
                  </span>
                </div>
              </div>
            </CardContent>
          </Card>
        </>
      )}

      {tab === 'sessions' && (
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <CardTitle>{t(AdminKeys.UsersEdit.ActiveSessionsTitle)}</CardTitle>
              {activeSessions.length > 0 && (
                <Button variant="danger" size="sm" onClick={() => setConfirmAction('revokeAll')}>
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
                        <TableCell className="text-sm">
                          {session.applicationName || '\u2014'}
                        </TableCell>
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
                              router.delete(`/admin/users/${user.id}/sessions/${session.tokenId}`)
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
