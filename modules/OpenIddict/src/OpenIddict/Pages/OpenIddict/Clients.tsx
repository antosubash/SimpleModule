import { router } from '@inertiajs/react';
import {
  Badge,
  Button,
  Card,
  CardContent,
  Container,
  DataGrid,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  PageHeader,
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
    <Container className="space-y-6">
      <PageHeader
        className="mb-0"
        title="Clients"
        description={`${clients.length} registered ${clients.length === 1 ? 'client' : 'clients'}`}
        actions={
          <Button onClick={() => router.get('/openiddict/clients/create')}>Create Client</Button>
        }
      />

      {clients.length === 0 ? (
        <Card>
          <CardContent>
            <div className="flex flex-col items-center justify-center py-12 text-center">
              <svg
                className="mb-4 h-12 w-12 text-text-muted/50"
                fill="none"
                stroke="currentColor"
                strokeWidth="1.5"
                viewBox="0 0 24 24"
                aria-hidden="true"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  d="M15.75 5.25a3 3 0 013 3m3 0a6 6 0 01-7.029 5.912c-.563-.097-1.159.026-1.563.43L10.5 17.25H8.25v2.25H6v2.25H2.25v-2.818c0-.597.237-1.17.659-1.591l6.499-6.499c.404-.404.527-1 .43-1.563A6 6 0 1121.75 8.25z"
                />
              </svg>
              <h3 className="text-sm font-medium">No clients yet</h3>
              <p className="mt-1 text-sm text-text-muted">
                Get started by registering your first OpenID Connect client.
              </p>
            </div>
          </CardContent>
        </Card>
      ) : (
        <DataGrid data={clients}>
          {(pageData) => (
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
                {pageData.map((client) => (
                  <TableRow key={client.id}>
                    <TableCell className="font-mono text-sm">{client.clientId}</TableCell>
                    <TableCell>{client.displayName || '\u2014'}</TableCell>
                    <TableCell>
                      <Badge
                        variant={client.clientType === 'confidential' ? 'default' : 'secondary'}
                      >
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
                          onClick={() =>
                            setDeleteTarget({ id: client.id, clientId: client.clientId })
                          }
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
        </DataGrid>
      )}

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
    </Container>
  );
}
