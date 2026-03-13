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
    <div>
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1
            className="text-2xl font-extrabold tracking-tight"
            style={{ fontFamily: "'Sora', sans-serif" }}
          >
            <span className="gradient-text">Roles</span>
          </h1>
          <p className="text-text-muted text-sm mt-1">Manage application roles</p>
        </div>
        <button onClick={() => router.get('/admin/roles/create')} className="btn-primary">
          Create Role
        </button>
      </div>

      <div className="glass-card overflow-x-auto">
        <table className="w-full text-left">
          <thead>
            <tr>
              <th className="px-4 py-3">Name</th>
              <th className="px-4 py-3">Description</th>
              <th className="px-4 py-3">Users</th>
              <th className="px-4 py-3">Created</th>
              <th className="px-4 py-3"></th>
            </tr>
          </thead>
          <tbody>
            {roles.map((role) => (
              <tr key={role.id} className="hover:bg-surface-raised transition-colors">
                <td className="px-4 py-3 font-medium text-text">{role.name}</td>
                <td className="px-4 py-3 text-text-secondary">{role.description || '\u2014'}</td>
                <td className="px-4 py-3">
                  <span className="badge-info">{role.userCount}</span>
                </td>
                <td className="px-4 py-3 text-sm text-text-muted">
                  {new Date(role.createdAt).toLocaleDateString()}
                </td>
                <td className="px-4 py-3">
                  <div className="flex gap-3">
                    <button
                      onClick={() => router.get(`/admin/roles/${role.id}/edit`)}
                      className="text-primary hover:text-primary-hover text-sm font-medium bg-transparent border-none cursor-pointer"
                    >
                      Edit
                    </button>
                    <button
                      onClick={() => handleDelete(role.id, role.name)}
                      className="text-danger hover:text-danger-hover text-sm font-medium bg-transparent border-none cursor-pointer"
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
