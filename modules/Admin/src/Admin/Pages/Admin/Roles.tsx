import { router } from '@inertiajs/react';
import {
  Badge,
  Button,
  DataGridPage,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';
import { useState } from 'react';

interface Role {
  id: string;
  name: string;
  description: string | null;
  userCount: number;
  permissionCount: number;
  createdAt: string;
}

interface Props {
  roles: Role[];
}

export default function Roles({ roles }: Props) {
  const [deleteTarget, setDeleteTarget] = useState<{
    id: string;
    name: string;
  } | null>(null);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  function handleDelete() {
    if (!deleteTarget) return;
    router.delete(`/admin/roles/${deleteTarget.id}`, {
      onError: () => {
        setDeleteTarget(null);
        setDeleteError('Cannot delete role with assigned users.');
      },
      onSuccess: () => setDeleteTarget(null),
    });
  }

  const errorAlert = deleteError ? (
    <div className="rounded-lg border border-danger/30 bg-danger/10 px-4 py-3 text-sm text-danger flex items-center justify-between">
      <span>{deleteError}</span>
      <button
        type="button"
        className="text-danger hover:text-danger/80"
        onClick={() => setDeleteError(null)}
      >
        <svg
          className="w-4 h-4"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          viewBox="0 0 24 24"
          aria-hidden="true"
        >
          <path d="M18 6 6 18M6 6l12 12" />
        </svg>
      </button>
    </div>
  ) : null;

  return (
    <>
      <DataGridPage
        title="Roles"
        description="Manage application roles and permissions."
        actions={<Button onClick={() => router.get('/admin/roles/create')}>Create Role</Button>}
        data={roles}
        filterBar={errorAlert}
      >
        {(pageData) => (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>Description</TableHead>
                <TableHead>Users</TableHead>
                <TableHead>Permissions</TableHead>
                <TableHead>Created</TableHead>
                <TableHead />
              </TableRow>
            </TableHeader>
            <TableBody>
              {pageData.map((role) => (
                <TableRow key={role.id}>
                  <TableCell className="font-medium">{role.name}</TableCell>
                  <TableCell className="text-text-secondary">
                    {role.description || '\u2014'}
                  </TableCell>
                  <TableCell>
                    <Badge variant="info">{role.userCount}</Badge>
                  </TableCell>
                  <TableCell>
                    <Badge variant="secondary">{role.permissionCount}</Badge>
                  </TableCell>
                  <TableCell className="text-sm text-text-muted">
                    {new Date(role.createdAt).toLocaleDateString()}
                  </TableCell>
                  <TableCell>
                    <div className="flex gap-3">
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => router.get(`/admin/roles/${role.id}/edit`)}
                      >
                        Edit
                      </Button>
                      <Button
                        variant="danger"
                        size="sm"
                        onClick={() => setDeleteTarget({ id: role.id, name: role.name })}
                      >
                        Delete
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </DataGridPage>

      <Dialog open={deleteTarget !== null} onOpenChange={(open) => !open && setDeleteTarget(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete Role</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete &ldquo;{deleteTarget?.name}&rdquo;? This will remove
              the role from all users.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="secondary" onClick={() => setDeleteTarget(null)}>
              Cancel
            </Button>
            <Button variant="danger" onClick={handleDelete}>
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
