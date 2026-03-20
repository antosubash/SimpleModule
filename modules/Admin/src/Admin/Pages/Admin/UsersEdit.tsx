import { router } from '@inertiajs/react';
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Input,
  Label,
} from '@simplemodule/ui';
import { useState } from 'react';
import { ActivityTimeline } from '../components/ActivityTimeline';
import { PermissionGroups } from '../components/PermissionGroups';
import { TabNav } from '../components/TabNav';

interface UserDetail {
  id: string;
  displayName: string;
  email: string;
  emailConfirmed: boolean;
  twoFactorEnabled: boolean;
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

interface ActivityEntry {
  id: number;
  action: string;
  details: string | null;
  performedBy: string;
  timestamp: string;
}

interface Props {
  user: UserDetail;
  userRoles: string[];
  userPermissions: string[];
  allRoles: Role[];
  permissionsByModule: Record<string, string[]>;
  activityLog: ActivityEntry[];
  activityTotal: number;
  tab: string;
}

const tabs = [
  { id: 'details', label: 'Details' },
  { id: 'roles', label: 'Roles & Permissions' },
  { id: 'security', label: 'Security' },
  { id: 'activity', label: 'Activity' },
];

type ConfirmAction = 'deactivate' | 'reverify' | 'disable2fa' | null;

export default function UsersEdit({
  user,
  userRoles,
  userPermissions,
  allRoles,
  permissionsByModule,
  activityLog,
  activityTotal,
  tab,
}: Props) {
  const [activityEntries, setActivityEntries] = useState(activityLog);
  const [activityPage, setActivityPage] = useState(1);
  const [confirmAction, setConfirmAction] = useState<ConfirmAction>(null);
  const [passwordError, setPasswordError] = useState<string | null>(null);

  async function loadMoreActivity() {
    const nextPage = activityPage + 1;
    const res = await fetch(`/admin/users/${user.id}/activity?page=${nextPage}`);
    const data = await res.json();
    setActivityEntries((prev) => [...prev, ...data.entries]);
    setActivityPage(nextPage);
  }

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
  };

  const dialogConfig = confirmAction ? confirmDialogConfig[confirmAction] : null;

  return (
    <div className="max-w-3xl">
      <div className="flex items-center gap-3 mb-1">
        <a
          href="/admin/users"
          className="text-text-muted hover:text-text transition-colors no-underline"
        >
          <svg
            className="w-4 h-4"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            viewBox="0 0 24 24"
          >
            <path d="M15 19l-7-7 7-7" />
          </svg>
        </a>
        <h1 className="text-2xl font-extrabold tracking-tight">Edit User</h1>
        {user.isDeactivated && <Badge variant="secondary">Deactivated</Badge>}
      </div>
      <p className="text-text-muted text-sm ml-7 mb-6">
        Created: {new Date(user.createdAt).toLocaleString()}
        {user.lastLoginAt && (
          <span className="ml-4">Last login: {new Date(user.lastLoginAt).toLocaleString()}</span>
        )}
      </p>

      <TabNav tabs={tabs} activeTab={tab} baseUrl={`/admin/users/${user.id}/edit`} />

      {tab === 'details' && (
        <>
          <Card className="mb-6">
            <CardHeader>
              <CardTitle>Details</CardTitle>
            </CardHeader>
            <CardContent>
              <form
                onSubmit={(e) => {
                  e.preventDefault();
                  router.post(`/admin/users/${user.id}`, new FormData(e.currentTarget));
                }}
                className="space-y-4"
              >
                <div>
                  <Label htmlFor="displayName">Display Name</Label>
                  <Input id="displayName" name="displayName" defaultValue={user.displayName} />
                </div>
                <div>
                  <Label htmlFor="email">Email</Label>
                  <Input id="email" name="email" type="email" defaultValue={user.email} />
                </div>
                <div className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    name="emailConfirmed"
                    id="emailConfirmed"
                    defaultChecked={user.emailConfirmed}
                    className="h-5 w-5 rounded-md border border-border bg-surface accent-primary"
                  />
                  <Label htmlFor="emailConfirmed" className="mb-0">
                    Email confirmed
                  </Label>
                </div>
                <Button type="submit">Save Details</Button>
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
          <Card className="mb-6">
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
                      <input
                        type="checkbox"
                        name="roles"
                        value={role.name ?? ''}
                        id={`role-${role.id}`}
                        defaultChecked={userRoles.includes(role.name ?? '')}
                        className="h-5 w-5 rounded-md border border-border bg-surface accent-primary"
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
          <Card className="mb-6">
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
                className="space-y-4"
              >
                {passwordError && (
                  <div className="rounded-lg border border-danger/30 bg-danger/10 px-4 py-3 text-sm text-danger">
                    {passwordError}
                  </div>
                )}
                <div>
                  <Label htmlFor="newPassword">New Password</Label>
                  <Input id="newPassword" name="newPassword" type="password" required />
                </div>
                <div>
                  <Label htmlFor="confirmPassword">Confirm Password</Label>
                  <Input id="confirmPassword" name="confirmPassword" type="password" required />
                </div>
                <Button type="submit">Reset Password</Button>
              </form>
            </CardContent>
          </Card>

          <Card className="mb-6">
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

          <Card className="mb-6">
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

          <Card className="mb-6">
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
              <div className="grid grid-cols-2 gap-4 text-sm">
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
              </div>
            </CardContent>
          </Card>
        </>
      )}

      {tab === 'activity' && (
        <Card>
          <CardHeader>
            <CardTitle>Activity Log</CardTitle>
          </CardHeader>
          <CardContent>
            <ActivityTimeline
              entries={activityEntries}
              total={activityTotal}
              onLoadMore={loadMoreActivity}
            />
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
    </div>
  );
}
