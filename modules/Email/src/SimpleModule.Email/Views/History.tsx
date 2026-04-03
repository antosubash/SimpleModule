import {
  Badge,
  DataGridPage,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';
import type { EmailMessage } from '../types';

function statusVariant(status: string): 'default' | 'secondary' | 'destructive' | 'outline' {
  switch (status) {
    case 'Sent':
      return 'default';
    case 'Failed':
      return 'destructive';
    case 'Sending':
    case 'Retrying':
      return 'secondary';
    default:
      return 'outline';
  }
}

export default function History({ messages }: { messages: EmailMessage[] }) {
  return (
    <DataGridPage title="Email History" description="View sent emails and their delivery status.">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>To</TableHead>
            <TableHead>Subject</TableHead>
            <TableHead>Status</TableHead>
            <TableHead>Provider</TableHead>
            <TableHead>Sent At</TableHead>
            <TableHead>Error</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {messages.map((m) => (
            <TableRow key={m.id}>
              <TableCell className="font-medium">{m.to}</TableCell>
              <TableCell>{m.subject}</TableCell>
              <TableCell>
                <Badge variant={statusVariant(m.status)}>{m.status}</Badge>
              </TableCell>
              <TableCell className="text-text-muted">{m.provider ?? '-'}</TableCell>
              <TableCell className="text-text-muted">
                {m.sentAt ? new Date(m.sentAt).toLocaleString() : '-'}
              </TableCell>
              <TableCell className="text-destructive max-w-[200px] truncate">
                {m.errorMessage ?? '-'}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </DataGridPage>
  );
}
