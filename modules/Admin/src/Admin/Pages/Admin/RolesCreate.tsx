import { router } from '@inertiajs/react';
import { AdminLayout } from '@simplemodule/client/admin-layout';
import { Button, Card, CardContent, Input, Label } from '@simplemodule/ui';
import { PermissionGroups } from '../components/PermissionGroups';

interface Props {
  permissionsByModule: Record<string, string[]>;
}

export default function RolesCreate({ permissionsByModule }: Props) {
  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    router.post('/admin/roles', new FormData(e.currentTarget));
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
        <h1 className="text-2xl font-extrabold tracking-tight">Create Role</h1>
      </div>
      <p className="text-text-muted text-sm ml-7 mb-6">
        Add a new application role with permissions
      </p>

      <Card>
        <CardContent className="p-6">
          <form onSubmit={handleSubmit} className="space-y-6">
            <div>
              <Label htmlFor="name">Name</Label>
              <Input id="name" name="name" required />
            </div>
            <div>
              <Label htmlFor="description">Description</Label>
              <Input id="description" name="description" />
            </div>
            <div>
              <Label>Permissions</Label>
              <div className="mt-2">
                <PermissionGroups
                  permissionsByModule={permissionsByModule}
                  selected={[]}
                  namePrefix="permissions"
                />
              </div>
            </div>
            <Button type="submit">Create Role</Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}

RolesCreate.layout = (page: React.ReactNode) => <AdminLayout>{page}</AdminLayout>;
