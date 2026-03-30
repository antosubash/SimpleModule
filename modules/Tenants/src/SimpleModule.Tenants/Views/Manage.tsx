import { router } from '@inertiajs/react';
import {
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

interface Tenant {
  id: number;
  name: string;
  slug: string;
  status: number;
  adminEmail: string | null;
  editionName: string | null;
  hosts: { id: number; hostName: string; isActive: boolean }[];
}

const statusLabels: Record<number, string> = { 0: 'Active', 1: 'Suspended', 2: 'Inactive' };
const statusColors: Record<number, string> = {
  0: 'text-green-600',
  1: 'text-yellow-600',
  2: 'text-red-600',
};

export default function Manage({ tenants }: { tenants: Tenant[] }) {
  const [deleteTarget, setDeleteTarget] = useState<{ id: number; name: string } | null>(null);

  function handleDelete() {
    if (!deleteTarget) return;
    router.delete(`/api/tenants/${deleteTarget.id}`);
    setDeleteTarget(null);
  }

  return (
    <>
      <DataGridPage
        title="Manage Tenants"
        description={`${tenants.length} total tenants`}
        actions={<Button onClick={() => router.get('/tenants/create')}>Create Tenant</Button>}
        data={tenants}
        emptyTitle="No tenants yet"
        emptyDescription="Get started by creating your first tenant."
      >
        {(pageData) => (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>ID</TableHead>
                <TableHead>Name</TableHead>
                <TableHead>Slug</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Hosts</TableHead>
                <TableHead>Edition</TableHead>
                <TableHead />
              </TableRow>
            </TableHeader>
            <TableBody>
              {pageData.map((tenant) => (
                <TableRow key={tenant.id}>
                  <TableCell className="text-text-muted">#{tenant.id}</TableCell>
                  <TableCell className="font-medium text-text">{tenant.name}</TableCell>
                  <TableCell className="text-text-muted">{tenant.slug}</TableCell>
                  <TableCell>
                    <span className={`font-medium ${statusColors[tenant.status]}`}>
                      {statusLabels[tenant.status]}
                    </span>
                  </TableCell>
                  <TableCell className="text-text-muted">{tenant.hosts.length}</TableCell>
                  <TableCell className="text-text-muted">{tenant.editionName ?? '-'}</TableCell>
                  <TableCell>
                    <div className="flex gap-3">
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => router.get(`/tenants/${tenant.id}/edit`)}
                      >
                        Edit
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => router.get(`/tenants/${tenant.id}/features`)}
                      >
                        Features
                      </Button>
                      <Button
                        variant="danger"
                        size="sm"
                        onClick={() => setDeleteTarget({ id: tenant.id, name: tenant.name })}
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
            <DialogTitle>Delete Tenant</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete &ldquo;{deleteTarget?.name}&rdquo;? This action cannot
              be undone.
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
