import { router } from '@inertiajs/react';
import { useState, FormEvent } from 'react';

interface User {
  id: string;
  displayName: string;
  email: string;
  emailConfirmed: boolean;
  roles: string[];
  isLockedOut: boolean;
  createdAt: string;
}

interface Props {
  users: User[];
  search: string;
  page: number;
  totalPages: number;
  totalCount: number;
}

export default function Users({ users, search, page, totalPages, totalCount }: Props) {
  const [searchValue, setSearchValue] = useState(search);

  function handleSearch(e: FormEvent) {
    e.preventDefault();
    router.get('/admin/users', { search: searchValue, page: 1 }, { preserveState: true });
  }

  function goToPage(p: number) {
    router.get('/admin/users', { search: searchValue, page: p }, { preserveState: true });
  }

  return (
    <div className="max-w-6xl mx-auto p-8">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-3xl font-bold">Users</h1>
        <span className="text-gray-500">{totalCount} total</span>
      </div>

      <form onSubmit={handleSearch} className="mb-6 flex gap-2">
        <input
          type="text"
          value={searchValue}
          onChange={(e) => setSearchValue(e.target.value)}
          placeholder="Search by name or email..."
          className="flex-1 px-4 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
        <button
          type="submit"
          className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
        >
          Search
        </button>
      </form>

      <div className="overflow-x-auto rounded-lg border border-gray-200 dark:border-gray-700">
        <table className="w-full text-left">
          <thead className="bg-gray-50 dark:bg-gray-800">
            <tr>
              <th className="px-4 py-3 text-sm font-medium text-gray-500 dark:text-gray-400">Name</th>
              <th className="px-4 py-3 text-sm font-medium text-gray-500 dark:text-gray-400">Email</th>
              <th className="px-4 py-3 text-sm font-medium text-gray-500 dark:text-gray-400">Roles</th>
              <th className="px-4 py-3 text-sm font-medium text-gray-500 dark:text-gray-400">Status</th>
              <th className="px-4 py-3 text-sm font-medium text-gray-500 dark:text-gray-400">Created</th>
              <th className="px-4 py-3 text-sm font-medium text-gray-500 dark:text-gray-400"></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
            {users.map((user) => (
              <tr key={user.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                <td className="px-4 py-3 font-medium">{user.displayName || '—'}</td>
                <td className="px-4 py-3 text-gray-600 dark:text-gray-400">
                  {user.email}
                  {!user.emailConfirmed && (
                    <span className="ml-2 text-xs text-amber-600 dark:text-amber-400">unverified</span>
                  )}
                </td>
                <td className="px-4 py-3">
                  <div className="flex gap-1 flex-wrap">
                    {user.roles.map((role) => (
                      <span
                        key={role}
                        className="px-2 py-0.5 text-xs rounded-full bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-300"
                      >
                        {role}
                      </span>
                    ))}
                  </div>
                </td>
                <td className="px-4 py-3">
                  {user.isLockedOut ? (
                    <span className="text-red-600 dark:text-red-400 text-sm">Locked</span>
                  ) : (
                    <span className="text-green-600 dark:text-green-400 text-sm">Active</span>
                  )}
                </td>
                <td className="px-4 py-3 text-sm text-gray-500">
                  {new Date(user.createdAt).toLocaleDateString()}
                </td>
                <td className="px-4 py-3">
                  <button
                    onClick={() => router.get(`/admin/users/${user.id}/edit`)}
                    className="text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-300 text-sm"
                  >
                    Edit
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {totalPages > 1 && (
        <div className="flex justify-center gap-2 mt-6">
          <button
            onClick={() => goToPage(page - 1)}
            disabled={page <= 1}
            className="px-3 py-1 rounded border border-gray-300 dark:border-gray-600 disabled:opacity-50"
          >
            Previous
          </button>
          <span className="px-3 py-1 text-gray-600 dark:text-gray-400">
            Page {page} of {totalPages}
          </span>
          <button
            onClick={() => goToPage(page + 1)}
            disabled={page >= totalPages}
            className="px-3 py-1 rounded border border-gray-300 dark:border-gray-600 disabled:opacity-50"
          >
            Next
          </button>
        </div>
      )}
    </div>
  );
}
