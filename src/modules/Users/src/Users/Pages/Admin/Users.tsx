import { router } from '@inertiajs/react';
import { type FormEvent, useState } from 'react';

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
    <div>
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1
            className="text-2xl font-extrabold tracking-tight"
            style={{ fontFamily: "'Sora', sans-serif" }}
          >
            <span className="gradient-text">Users</span>
          </h1>
          <p className="text-text-muted text-sm mt-1">{totalCount} total users</p>
        </div>
      </div>

      <form onSubmit={handleSearch} className="mb-6 flex gap-2">
        <input
          type="text"
          value={searchValue}
          onChange={(e) => setSearchValue(e.target.value)}
          placeholder="Search by name or email..."
          className="flex-1"
        />
        <button type="submit" className="btn-primary">
          Search
        </button>
      </form>

      <div className="glass-card overflow-x-auto">
        <table className="w-full text-left">
          <thead>
            <tr>
              <th className="px-4 py-3">Name</th>
              <th className="px-4 py-3">Email</th>
              <th className="px-4 py-3">Roles</th>
              <th className="px-4 py-3">Status</th>
              <th className="px-4 py-3">Created</th>
              <th className="px-4 py-3"></th>
            </tr>
          </thead>
          <tbody>
            {users.map((user) => (
              <tr key={user.id} className="hover:bg-surface-raised transition-colors">
                <td className="px-4 py-3 font-medium text-text">{user.displayName || '\u2014'}</td>
                <td className="px-4 py-3 text-text-secondary">
                  {user.email}
                  {!user.emailConfirmed && <span className="ml-2 badge-warning">unverified</span>}
                </td>
                <td className="px-4 py-3">
                  <div className="flex gap-1 flex-wrap">
                    {user.roles.map((role) => (
                      <span key={role} className="badge-info">
                        {role}
                      </span>
                    ))}
                  </div>
                </td>
                <td className="px-4 py-3">
                  {user.isLockedOut ? (
                    <span className="badge-danger">Locked</span>
                  ) : (
                    <span className="badge-success">Active</span>
                  )}
                </td>
                <td className="px-4 py-3 text-sm text-text-muted">
                  {new Date(user.createdAt).toLocaleDateString()}
                </td>
                <td className="px-4 py-3">
                  <button
                    onClick={() => router.get(`/admin/users/${user.id}/edit`)}
                    className="text-primary hover:text-primary-hover text-sm font-medium bg-transparent border-none cursor-pointer"
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
            className="btn-secondary btn-sm disabled:opacity-50"
          >
            Previous
          </button>
          <span className="px-3 py-1 text-text-muted text-sm">
            Page {page} of {totalPages}
          </span>
          <button
            onClick={() => goToPage(page + 1)}
            disabled={page >= totalPages}
            className="btn-secondary btn-sm disabled:opacity-50"
          >
            Next
          </button>
        </div>
      )}
    </div>
  );
}
