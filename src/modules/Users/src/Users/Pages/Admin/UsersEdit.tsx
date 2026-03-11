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
    <div className="max-w-3xl mx-auto p-8">
      <div className="flex items-center gap-4 mb-6">
        <button
          onClick={() => router.get('/admin/users')}
          className="text-gray-500 hover:text-gray-700 dark:hover:text-gray-300"
        >
          &larr; Back
        </button>
        <h1 className="text-3xl font-bold">Edit User</h1>
      </div>

      <div className="mb-6 text-sm text-gray-500 dark:text-gray-400">
        <span>Created: {new Date(user.createdAt).toLocaleString()}</span>
        {user.lastLoginAt && (
          <span className="ml-4">Last login: {new Date(user.lastLoginAt).toLocaleString()}</span>
        )}
      </div>

      {/* User Details Form */}
      <form onSubmit={handleSubmit} className="mb-8 p-6 rounded-lg border border-gray-200 dark:border-gray-700">
        <h2 className="text-lg font-semibold mb-4">Details</h2>
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1">Display Name</label>
            <input
              type="text"
              name="displayName"
              defaultValue={user.displayName}
              className="w-full px-4 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">Email</label>
            <input
              type="email"
              name="email"
              defaultValue={user.email}
              className="w-full px-4 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <div className="flex items-center gap-2">
            <input
              type="checkbox"
              name="emailConfirmed"
              id="emailConfirmed"
              defaultChecked={user.emailConfirmed}
              className="rounded border-gray-300"
            />
            <label htmlFor="emailConfirmed" className="text-sm">Email confirmed</label>
          </div>
          <button
            type="submit"
            className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
          >
            Save Details
          </button>
        </div>
      </form>

      {/* Roles Form */}
      <form onSubmit={handleRolesSubmit} className="mb-8 p-6 rounded-lg border border-gray-200 dark:border-gray-700">
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
                className="rounded border-gray-300"
              />
              <label htmlFor={`role-${role.id}`} className="text-sm">
                {role.name}
                {role.description && (
                  <span className="text-gray-500 ml-1">— {role.description}</span>
                )}
              </label>
            </div>
          ))}
          {allRoles.length === 0 && (
            <p className="text-sm text-gray-500">No roles defined.</p>
          )}
        </div>
        <button
          type="submit"
          className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
        >
          Save Roles
        </button>
      </form>

      {/* Lock/Unlock */}
      <div className="p-6 rounded-lg border border-gray-200 dark:border-gray-700">
        <h2 className="text-lg font-semibold mb-4">Account Status</h2>
        {user.isLockedOut ? (
          <div>
            <p className="text-sm text-red-600 dark:text-red-400 mb-3">This account is locked.</p>
            <button
              onClick={handleUnlock}
              className="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700"
            >
              Unlock Account
            </button>
          </div>
        ) : (
          <div>
            <p className="text-sm text-green-600 dark:text-green-400 mb-3">This account is active.</p>
            <button
              onClick={handleLock}
              className="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700"
            >
              Lock Account
            </button>
          </div>
        )}
      </div>
    </div>
  );
}
