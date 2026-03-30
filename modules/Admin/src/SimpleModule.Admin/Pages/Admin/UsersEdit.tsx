import { router } from '@inertiajs/react';
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

const tabs = [
  { id: 'details', label: 'Details' },
  { id: 'roles', label: 'Roles & Permissions' },
  { id: 'security', label: 'Security' },
  { id: 'sessions', label: 'Sessions' },
];

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
  const [confirmAction, setConfirmAction] = useState<ConfirmAction>(null);
  const [passwordError, setPasswordError] = useState<string | null>(null);

  const isSelf = user.id === currentUserId;

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
      title: 'Deactivate Account',
      description: 'This user will no longer be able to sign in. Are you sure?',
      action: 'Deactivate',
    },
    reverify: {
      title: 'Force Re-verification',
      description: 'This will require the user to re-verify their email address. Are you sure?',
      action: 'Force Re-verification',
    },
    disable2fa: {
      title: 'Disable Two-Factor Authentication',
      description: 'This will disable 2FA and reset the authenticator for this user. Are you sure?',
      action: 'Disable 2FA',
    },
    revokeAll: {
      title: 'Revoke All Sessions',
      description:
        'This will invalidate all active sessions for this user. They will need to sign in again.',
      action: 'Revoke All',
    },
  };

  const dialogConfig = confirmAction ? confirmDialogConfig[confirmAction] : null;

  return (
    <Container className="space-y-6">
      <Breadcrumb>
        <BreadcrumbList>
          <BreadcrumbItem>
            <BreadcrumbLink href="/admin/users">Users</BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbPage>Edit User</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>
      <div className="flex items-center gap-3">
        <h1 className="text-2xl font-bold tracking-tight">Edit User</h1>
        {isSelf && <Badge variant="info">You</Badge>}
        {user.isDeactivated && <Badge variant="secondary">Deactivated</Badge>}
        {user.isLockedOut && !user.isDeactivated && <Badge variant="danger">Locked</Badge>}
      </div>

      <TabNav tabs={tabs} activeTab={tab} baseUrl={`/admin/users/${user.id}/edit`} />

      {tab === 'details' && (
        <>
          <Card>
            <CardHeader>
              <CardTitle>Details</CardTitle>
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
                    <Label htmlFor="displayName">Display Name</Label>
                    <Input id="displayName" name="displayName" defaultValue={user.displayName} />
                  </Field>
                  <Field>
                    <Label htmlFor="email">Email</Label>
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
                      Email confirmed
                    </Label>
                  </Field>
                  <Button type="submit">Save Details</Button>
                </FieldGroup>
              </form>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Account Status</CardTitle>
            </CardHeader>
            <CardContent>
              {user.isDeactivated ? (
                <div>
                  <p className="text-sm text-text-muted mb-3">This account has been deactivated.</p>
                  <Button
                    variant="outline"
                    onClick={() => router.post(`/admin/users/${user.id}/reactivate`)}
                  >
                    Reactivate Account
                  </Button>
                </div>
              ) : isSelf ? (
                <p className="text-sm text-text-muted">You cannot deactivate your own account.</p>
              ) : (
                <div>
                  <p className="text-sm text-text-muted mb-3">
                    Deactivating will lock the account and prevent login.
                  </p>
                  <Button variant="danger" onClick={() => setConfirmAction('deactivate')}>
                    Deactivate Account
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
              <CardTitle>Roles</CardTitle>
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
                    <p className="text-sm text-text-muted">No roles defined.</p>
                  )}
                </div>
                <Button type="submit">Save Roles</Button>
              </form>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Direct Permissions</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-sm text-text-muted mb-4">
                These permissions are granted directly to this user, bypassing role assignments.
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
                  Save Permissions
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
              <CardTitle>Reset Password</CardTitle>
            </CardHeader>
            <CardContent>
              <form
                onSubmit={(e) => {
                  e.preventDefault();
                  const formData = new FormData(e.currentTarget);
                  if (formData.get('newPassword') !== formData.get('confirmPassword')) {
                    setPasswordError('Passwords do not match.');
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
                    <Label htmlFor="newPassword">New Password</Label>
                    <Input id="newPassword" name="newPassword" type="password" required />
                  </Field>
                  <Field>
                    <Label htmlFor="confirmPassword">Confirm Password</Label>
                    <Input id="confirmPassword" name="confirmPassword" type="password" required />
                  </Field>
                  <Button type="submit">Reset Password</Button>
                </FieldGroup>
              </form>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Account Lock</CardTitle>
            </CardHeader>
            <CardContent>
              {user.isLockedOut ? (
                <div>
                  <p className="text-sm text-danger mb-3">This account is locked.</p>
                  <Button
                    variant="outline"
                    onClick={() => router.post(`/admin/users/${user.id}/unlock`)}
                  >
                    Unlock Account
                  </Button>
                </div>
              ) : isSelf ? (
                <p className="text-sm text-text-muted">You cannot lock your own account.</p>
              ) : (
                <div>
                  <p className="text-sm text-success mb-3">This account is active.</p>
                  <Button
                    variant="danger"
                    onClick={() => router.post(`/admin/users/${user.id}/lock`)}
                  >
                    Lock Account
                  </Button>
                </div>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Email Verification</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-sm text-text-muted mb-3">
                Status: {user.emailConfirmed ? 'Verified' : 'Not verified'}
              </p>
              {user.emailConfirmed && (
                <Button variant="outline" onClick={() => setConfirmAction('reverify')}>
                  Force Re-verification
                </Button>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Two-Factor Authentication</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-sm text-text-muted mb-3">
                Status: {user.twoFactorEnabled ? 'Enabled' : 'Not enabled'}
              </p>
              {user.twoFactorEnabled && (
                <Button variant="danger" onClick={() => setConfirmAction('disable2fa')}>
                  Disable 2FA
                </Button>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Login Info</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 text-sm">
                <div>
                  <span className="text-text-muted">Failed login attempts:</span>
                  <span className="ml-2 font-medium">{user.accessFailedCount}</span>
                </div>
                <div>
                  <span className="text-text-muted">Last login:</span>
                  <span className="ml-2 font-medium">
                    {user.lastLoginAt ? new Date(user.lastLoginAt).toLocaleString() : 'Never'}
                  </span>
                </div>
                <div>
                  <span className="text-text-muted">Created:</span>
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
              <CardTitle>Active Sessions</CardTitle>
              {activeSessions.length > 0 && (
                <Button variant="danger" size="sm" onClick={() => setConfirmAction('revokeAll')}>
                  Revoke All
                </Button>
              )}
            </div>
          </CardHeader>
          <CardContent>
            {activeSessions.length === 0 ? (
              <p className="text-sm text-text-muted">No active sessions.</p>
            ) : (
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
                  {activeSessions.map((session) => (
                    <TableRow key={session.tokenId}>
                      <TableCell>
                        <Badge variant={session.type === 'refresh_token' ? 'info' : 'secondary'}>
                          {session.type === 'refresh_token' ? 'Refresh' : 'Access'}
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
                          : 'Never'}
                      </TableCell>
                      <TableCell>
                        <Button
                          variant="danger"
                          size="sm"
                          onClick={() =>
                            router.delete(`/admin/users/${user.id}/sessions/${session.tokenId}`)
                          }
                        >
                          Revoke
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
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
          <DialogFooter>
            <Button variant="secondary" onClick={() => setConfirmAction(null)}>
              Cancel
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
