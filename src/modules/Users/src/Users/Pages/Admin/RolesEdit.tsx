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
    <div className="max-w-xl">
      <div className="flex items-center gap-3 mb-1">
        <a
          href="/admin/roles"
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
        <h1
          className="text-2xl font-extrabold tracking-tight"
          style={{ fontFamily: "'Sora', sans-serif" }}
        >
          <span className="gradient-text">Edit Role</span>
        </h1>
      </div>
      <p className="text-text-muted text-sm ml-7 mb-6">
        Created: {new Date(role.createdAt).toLocaleString()}
      </p>

      <form onSubmit={handleSubmit} className="glass-card p-6 mb-6">
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1">Name</label>
            <input type="text" name="name" defaultValue={role.name} required />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">Description</label>
            <input type="text" name="description" defaultValue={role.description ?? ''} />
          </div>
          <button type="submit" className="btn-primary">
            Save
          </button>
        </div>
      </form>

      <div className="glass-card p-6">
        <h2 className="text-lg font-semibold mb-4">Assigned Users ({users.length})</h2>
        {users.length === 0 ? (
          <p className="text-sm text-text-muted">No users assigned to this role.</p>
        ) : (
          <ul className="space-y-2" style={{ listStyle: 'none' }}>
            {users.map((user) => (
              <li
                key={user.id}
                className="flex justify-between items-center py-2 border-t border-border"
              >
                <div>
                  <span className="font-medium text-text">{user.displayName || '\u2014'}</span>
                  <span className="text-text-muted ml-2 text-sm">{user.email}</span>
                </div>
                <button
                  onClick={() => router.get(`/admin/users/${user.id}/edit`)}
                  className="text-primary hover:text-primary-hover text-sm font-medium bg-transparent border-none cursor-pointer"
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
