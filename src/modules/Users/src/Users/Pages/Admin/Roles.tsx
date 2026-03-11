import { router } from '@inertiajs/react';

interface Role {
  id: string;
  name: string;
  description: string | null;
  userCount: number;
  createdAt: string;
}

interface Props {
  roles: Role[];
}

export default function Roles({ roles }: Props) {
  function handleDelete(id: string, name: string) {
    if (!confirm(`Delete role "${name}"?`)) return;
    router.delete(`/admin/roles/${id}`, {
      onError: () => alert('Cannot delete role with assigned users.'),
    });
  }

  return (
    <div className="max-w-4xl mx-auto p-8">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-3xl font-bold">Roles</h1>
        <button
          onClick={() => router.get('/admin/roles/create')}
          className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
        >
          Create Role
        </button>
      </div>

      <div className="overflow-x-auto rounded-lg border border-gray-200 dark:border-gray-700">
        <table className="w-full text-left">
          <thead className="bg-gray-50 dark:bg-gray-800">
            <tr>
              <th className="px-4 py-3 text-sm font-medium text-gray-500 dark:text-gray-400">Name</th>
              <th className="px-4 py-3 text-sm font-medium text-gray-500 dark:text-gray-400">Description</th>
              <th className="px-4 py-3 text-sm font-medium text-gray-500 dark:text-gray-400">Users</th>
              <th className="px-4 py-3 text-sm font-medium text-gray-500 dark:text-gray-400">Created</th>
              <th className="px-4 py-3 text-sm font-medium text-gray-500 dark:text-gray-400"></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
            {roles.map((role) => (
              <tr key={role.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                <td className="px-4 py-3 font-medium">{role.name}</td>
                <td className="px-4 py-3 text-gray-600 dark:text-gray-400">
                  {role.description || '—'}
                </td>
                <td className="px-4 py-3">
                  <span className="px-2 py-0.5 text-xs rounded-full bg-gray-100 dark:bg-gray-800">
                    {role.userCount}
                  </span>
                </td>
                <td className="px-4 py-3 text-sm text-gray-500">
                  {new Date(role.createdAt).toLocaleDateString()}
                </td>
                <td className="px-4 py-3">
                  <div className="flex gap-3">
                    <button
                      onClick={() => router.get(`/admin/roles/${role.id}/edit`)}
                      className="text-blue-600 hover:text-blue-800 dark:text-blue-400 text-sm"
                    >
                      Edit
                    </button>
                    <button
                      onClick={() => handleDelete(role.id, role.name)}
                      className="text-red-600 hover:text-red-800 dark:text-red-400 text-sm"
                    >
                      Delete
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
