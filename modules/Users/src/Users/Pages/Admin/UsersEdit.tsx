import { router } from '@inertiajs/react';
import { Button, Card, CardContent, CardHeader, CardTitle, Input, Label } from '@simplemodule/ui';

interface UserDetail {
  id: string;
  displayName: string;
  email: string;
  emailConfirmed: boolean;
  isLockedOut: boolean;
  createdAt: string;
  lastLoginAt: string | null;
}

interface Role {
  id: string;
  name: string;
  description: string | null;
}

interface Props {
  user: UserDetail;
  userRoles: string[];
  allRoles: Role[];
}

export default function UsersEdit({ user, userRoles, allRoles }: Props) {
  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post(`/admin/users/${user.id}`, formData);
  }

  function handleRolesSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post(`/admin/users/${user.id}/roles`, formData);
  }

  function handleLock() {
    router.post(`/admin/users/${user.id}/lock`);
  }

  function handleUnlock() {
    router.post(`/admin/users/${user.id}/unlock`);
  }

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
      </div>
      <p className="text-text-muted text-sm ml-7 mb-6">
        Created: {new Date(user.createdAt).toLocaleString()}
        {user.lastLoginAt && (
          <span className="ml-4">Last login: {new Date(user.lastLoginAt).toLocaleString()}</span>
        )}
      </p>

      <Card className="mb-6">
        <CardHeader>
          <CardTitle>Details</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
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

      <Card className="mb-6">
        <CardHeader>
          <CardTitle>Roles</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleRolesSubmit}>
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
          <CardTitle>Account Status</CardTitle>
        </CardHeader>
        <CardContent>
          {user.isLockedOut ? (
            <div>
              <p className="text-sm text-danger mb-3">This account is locked.</p>
              <Button variant="outline" onClick={handleUnlock}>
                Unlock Account
              </Button>
            </div>
          ) : (
            <div>
              <p className="text-sm text-success mb-3">This account is active.</p>
              <Button variant="danger" onClick={handleLock}>
                Lock Account
              </Button>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
