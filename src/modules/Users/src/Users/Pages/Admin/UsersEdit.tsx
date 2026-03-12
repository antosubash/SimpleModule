import { router } from '@inertiajs/react';

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
        <a href="/admin/users" className="text-text-muted hover:text-text transition-colors no-underline">
          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path d="M15 19l-7-7 7-7"/></svg>
        </a>
        <h1 className="text-2xl font-extrabold tracking-tight" style={{ fontFamily: "'Sora', sans-serif" }}>
          <span className="gradient-text">Edit User</span>
        </h1>
      </div>
      <p className="text-text-muted text-sm ml-7 mb-6">
        Created: {new Date(user.createdAt).toLocaleString()}
        {user.lastLoginAt && (
          <span className="ml-4">Last login: {new Date(user.lastLoginAt).toLocaleString()}</span>
        )}
      </p>

      <form onSubmit={handleSubmit} className="glass-card p-6 mb-6">
        <h2 className="text-lg font-semibold mb-4">Details</h2>
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1">Display Name</label>
            <input type="text" name="displayName" defaultValue={user.displayName} />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">Email</label>
            <input type="email" name="email" defaultValue={user.email} />
          </div>
          <div className="flex items-center gap-2">
            <input
              type="checkbox"
              name="emailConfirmed"
              id="emailConfirmed"
              defaultChecked={user.emailConfirmed}
              className="accent-primary"
            />
            <label htmlFor="emailConfirmed" className="text-sm mb-0">Email confirmed</label>
          </div>
          <button type="submit" className="btn-primary">Save Details</button>
        </div>
      </form>

      <form onSubmit={handleRolesSubmit} className="glass-card p-6 mb-6">
        <h2 className="text-lg font-semibold mb-4">Roles</h2>
        <div className="space-y-2 mb-4">
          {allRoles.map((role) => (
            <div key={role.id} className="flex items-center gap-2">
              <input
                type="checkbox"
                name="roles"
                value={role.name ?? ''}
                id={`role-${role.id}`}
                defaultChecked={userRoles.includes(role.name ?? '')}
                className="accent-primary"
              />
              <label htmlFor={`role-${role.id}`} className="text-sm mb-0">
                {role.name}
                {role.description && (
                  <span className="text-text-muted ml-1">&mdash; {role.description}</span>
                )}
              </label>
            </div>
          ))}
          {allRoles.length === 0 && (
            <p className="text-sm text-text-muted">No roles defined.</p>
          )}
        </div>
        <button type="submit" className="btn-primary">Save Roles</button>
      </form>

      <div className="glass-card p-6">
        <h2 className="text-lg font-semibold mb-4">Account Status</h2>
        {user.isLockedOut ? (
          <div>
            <p className="text-sm text-danger mb-3">This account is locked.</p>
            <button onClick={handleUnlock} className="btn-primary" style={{ background: 'var(--color-success)' }}>
              Unlock Account
            </button>
          </div>
        ) : (
          <div>
            <p className="text-sm text-success mb-3">This account is active.</p>
            <button onClick={handleLock} className="btn-danger">Lock Account</button>
          </div>
        )}
      </div>
    </div>
  );
}
