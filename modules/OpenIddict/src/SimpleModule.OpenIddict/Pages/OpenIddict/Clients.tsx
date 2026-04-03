import { router } from '@inertiajs/react';
import { useTranslation } from '@simplemodule/client/use-translation';
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
import { OpenIddictKeys } from '../../Locales/keys';

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
  const { t } = useTranslation('OpenIddict');
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
    <>
      <DataGridPage
        title={t(OpenIddictKeys.Clients.Title)}
        description={
          clients.length === 1
            ? t(OpenIddictKeys.Clients.Description.Singular, { count: String(clients.length) })
            : t(OpenIddictKeys.Clients.Description.Plural, { count: String(clients.length) })
        }
        actions={
          <Button onClick={() => router.get('/openiddict/clients/create')}>
            {t(OpenIddictKeys.Clients.CreateButton)}
          </Button>
        }
        data={clients}
        emptyTitle={t(OpenIddictKeys.Clients.EmptyTitle)}
        emptyDescription={t(OpenIddictKeys.Clients.EmptyDescription)}
      >
        {(pageData) => (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t(OpenIddictKeys.Clients.ColClientId)}</TableHead>
                <TableHead>{t(OpenIddictKeys.Clients.ColDisplayName)}</TableHead>
                <TableHead>{t(OpenIddictKeys.Clients.ColType)}</TableHead>
                <TableHead />
              </TableRow>
            </TableHeader>
            <TableBody>
              {pageData.map((client) => (
                <TableRow key={client.id}>
                  <TableCell className="font-mono text-sm">{client.clientId}</TableCell>
                  <TableCell>{client.displayName || '\u2014'}</TableCell>
                  <TableCell>
                    <Badge variant={client.clientType === 'confidential' ? 'default' : 'info'}>
                      {client.clientType || 'public'}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    <div className="flex flex-wrap gap-2">
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => router.get(`/openiddict/clients/${client.id}/edit`)}
                      >
                        {t(OpenIddictKeys.Clients.EditButton)}
                      </Button>
                      <Button
                        variant="danger"
                        size="sm"
                        onClick={() =>
                          setDeleteTarget({ id: client.id, clientId: client.clientId })
                        }
                      >
                        {t(OpenIddictKeys.Clients.DeleteButton)}
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
            <DialogTitle>{t(OpenIddictKeys.Clients.DeleteDialog.Title)}</DialogTitle>
            <DialogDescription>
              {t(OpenIddictKeys.Clients.DeleteDialog.Description, {
                clientId: deleteTarget?.clientId ?? '',
              })}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="secondary" onClick={() => setDeleteTarget(null)}>
              {t(OpenIddictKeys.Clients.DeleteDialog.CancelButton)}
            </Button>
            <Button variant="danger" onClick={handleDelete}>
              {t(OpenIddictKeys.Clients.DeleteDialog.DeleteButton)}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
