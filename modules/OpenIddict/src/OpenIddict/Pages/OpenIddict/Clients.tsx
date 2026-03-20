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
  function handleDelete(id: string, clientId: string) {
    if (!confirm(`Delete client "${clientId}"?`)) return;
    router.delete(`/openiddict/clients/${id}`);
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

      {clients.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-12 text-center">
          <svg
            className="mb-4 h-12 w-12 text-muted-foreground/50"
            fill="none"
            stroke="currentColor"
            strokeWidth="1.5"
            viewBox="0 0 24 24"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              d="M15.75 5.25a3 3 0 013 3m3 0a6 6 0 01-7.029 5.912c-.563-.097-1.159.026-1.563.43L10.5 17.25H8.25v2.25H6v2.25H2.25v-2.818c0-.597.237-1.17.659-1.591l6.499-6.499c.404-.404.527-1 .43-1.563A6 6 0 1121.75 8.25z"
            />
          </svg>
          <h3 className="text-sm font-medium text-foreground">No clients yet</h3>
          <p className="mt-1 text-sm text-muted-foreground">
            Get started by registering your first OpenID Connect client.
          </p>
          <Button
            size="sm"
            className="mt-4"
            onClick={() => router.get('/openiddict/clients/create')}
          >
            Create Client
          </Button>
        </div>
      ) : (
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
                      onClick={() => handleDelete(client.id, client.clientId)}
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
    </div>
  );
}
