import { router } from '@inertiajs/react';
import {
  Badge,
  Button,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';

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
  function handleDelete(id: string, name: string) {
    if (!confirm(`Delete role "${name}"?`)) return;
    router.delete(`/admin/roles/${id}`, {
      onError: () => alert('Cannot delete role with assigned users.'),
    });
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <div className="space-y-1">
          <h1 className="text-2xl font-bold tracking-tight">Roles</h1>
          <p className="text-sm text-muted-foreground">Manage application roles and permissions.</p>
        </div>
        <Button onClick={() => router.get('/admin/roles/create')}>Create Role</Button>
      </div>

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
          {roles.map((role) => (
            <TableRow key={role.id}>
              <TableCell className="font-medium">{role.name}</TableCell>
              <TableCell className="text-text-secondary">{role.description || '\u2014'}</TableCell>
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
                    onClick={() => handleDelete(role.id, role.name)}
                  >
                    Delete
                  </Button>
                </div>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}
