import { router } from '@inertiajs/react';
import {
  Badge,
  Button,
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

interface Client {
  id: string;
  clientId: string;
  displayName: string | null;
  clientType: string | null;
}

interface Props {
  clients: Client[];
}

export default function Clients({ clients }: Props) {
  const [deleteTarget, setDeleteTarget] = useState<{
    id: string;
    clientId: string;
  } | null>(null);

  function handleDelete() {
    if (!deleteTarget) return;
    router.delete(`/openiddict/clients/${deleteTarget.id}`);
    setDeleteTarget(null);
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1 className="text-2xl font-extrabold tracking-tight">Clients</h1>
          <p className="text-text-muted text-sm mt-1">
            {clients.length} registered {clients.length === 1 ? 'client' : 'clients'}
          </p>
        </div>
        <Button onClick={() => router.get('/openiddict/clients/create')}>Create Client</Button>
      </div>

      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Client ID</TableHead>
            <TableHead>Display Name</TableHead>
            <TableHead>Type</TableHead>
            <TableHead />
          </TableRow>
        </TableHeader>
        <TableBody>
          {clients.map((client) => (
            <TableRow key={client.id}>
              <TableCell className="font-mono text-sm">{client.clientId}</TableCell>
              <TableCell>{client.displayName || '\u2014'}</TableCell>
              <TableCell>
                <Badge variant={client.clientType === 'confidential' ? 'default' : 'secondary'}>
                  {client.clientType || 'public'}
                </Badge>
              </TableCell>
              <TableCell>
                <div className="flex gap-3">
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => router.get(`/openiddict/clients/${client.id}/edit`)}
                  >
                    Edit
                  </Button>
                  <Button
                    variant="danger"
                    size="sm"
                    onClick={() => setDeleteTarget({ id: client.id, clientId: client.clientId })}
                  >
                    Delete
                  </Button>
                </div>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>

      <Dialog open={deleteTarget !== null} onOpenChange={(open) => !open && setDeleteTarget(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete Client</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete &ldquo;{deleteTarget?.clientId}&rdquo;? This OAuth
              client will be permanently removed.
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
    </div>
  );
}
