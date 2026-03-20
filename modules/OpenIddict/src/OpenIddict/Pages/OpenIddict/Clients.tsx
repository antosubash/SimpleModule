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
        <div className="space-y-1">
          <h1 className="text-2xl font-bold tracking-tight">Clients</h1>
          <p className="text-sm text-muted-foreground">
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
    </div>
  );
}
