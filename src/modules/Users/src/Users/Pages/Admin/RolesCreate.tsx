import { router } from '@inertiajs/react';

export default function RolesCreate() {
  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post('/admin/roles', formData);
  }

  return (
    <div className="max-w-xl">
      <div className="flex items-center gap-3 mb-1">
        <a href="/admin/roles" className="text-text-muted hover:text-text transition-colors no-underline">
          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path d="M15 19l-7-7 7-7"/></svg>
        </a>
        <h1 className="text-2xl font-extrabold tracking-tight" style={{ fontFamily: "'Sora', sans-serif" }}>
          <span className="gradient-text">Create Role</span>
        </h1>
      </div>
      <p className="text-text-muted text-sm ml-7 mb-6">Add a new application role</p>

      <form onSubmit={handleSubmit} className="glass-card p-6">
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1">Name</label>
            <input type="text" name="name" required />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">Description</label>
            <input type="text" name="description" />
          </div>
          <button type="submit" className="btn-primary">Create</button>
        </div>
      </form>
    </div>
  );
}
