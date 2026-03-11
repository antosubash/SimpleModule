import { router } from '@inertiajs/react';

interface RoleDetail {
  id: string;
  name: string;
  description: string | null;
  createdAt: string;
}

interface UserSummary {
  id: string;
  displayName: string;
  email: string;
}

interface Props {
  role: RoleDetail;
  users: UserSummary[];
}

export default function RolesEdit({ role, users }: Props) {
  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post(`/admin/roles/${role.id}`, formData);
  }

  return (
    <div className="max-w-xl mx-auto p-8">
      <div className="flex items-center gap-4 mb-6">
        <button
          onClick={() => router.get('/admin/roles')}
          className="text-gray-500 hover:text-gray-700 dark:hover:text-gray-300"
        >
          &larr; Back
        </button>
        <h1 className="text-3xl font-bold">Edit Role</h1>
      </div>

      <form onSubmit={handleSubmit} className="mb-8 p-6 rounded-lg border border-gray-200 dark:border-gray-700">
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1">Name</label>
            <input
              type="text"
              name="name"
              defaultValue={role.name}
              required
              className="w-full px-4 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">Description</label>
            <input
              type="text"
              name="description"
              defaultValue={role.description ?? ''}
              className="w-full px-4 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <div className="text-sm text-gray-500">
            Created: {new Date(role.createdAt).toLocaleString()}
          </div>
          <button
            type="submit"
            className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
          >
            Save
          </button>
        </div>
      </form>

      <div className="p-6 rounded-lg border border-gray-200 dark:border-gray-700">
        <h2 className="text-lg font-semibold mb-4">Assigned Users ({users.length})</h2>
        {users.length === 0 ? (
          <p className="text-sm text-gray-500">No users assigned to this role.</p>
        ) : (
          <ul className="space-y-2">
            {users.map((user) => (
              <li key={user.id} className="flex justify-between items-center py-2">
                <div>
                  <span className="font-medium">{user.displayName || '—'}</span>
                  <span className="text-gray-500 ml-2 text-sm">{user.email}</span>
                </div>
                <button
                  onClick={() => router.get(`/admin/users/${user.id}/edit`)}
                  className="text-blue-600 hover:text-blue-800 dark:text-blue-400 text-sm"
                >
                  Edit
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}
