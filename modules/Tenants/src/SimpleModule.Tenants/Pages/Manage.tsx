import { router } from '@inertiajs/react';
import { routes } from '@simplemodule/client/routes';
import { useTranslation } from '@simplemodule/client/use-translation';
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
import { TenantsKeys } from '@/Locales/keys';
import type { Tenant } from '@/types';
import { statusColors, statusLabels } from './tenantStatus';

export default function Manage({ tenants }: { tenants: Tenant[] }) {
  const { t } = useTranslation('Tenants');
  const [deleteTarget, setDeleteTarget] = useState<{ id: number; name: string } | null>(null);

  function handleDelete() {
    if (!deleteTarget) return;
    router.delete(routes.tenants.api.delete(deleteTarget.id));
    setDeleteTarget(null);
  }

  return (
    <>
      <DataGridPage
        title={t(TenantsKeys.Manage.Title)}
        description={t(TenantsKeys.Manage.Description, { count: String(tenants.length) })}
        actions={
          <Button onClick={() => router.get(routes.tenants.views.create())}>
            {t(TenantsKeys.Manage.CreateButton)}
          </Button>
        }
        data={tenants}
        emptyTitle={t(TenantsKeys.Manage.EmptyTitle)}
        emptyDescription={t(TenantsKeys.Manage.EmptyDescription)}
      >
        {(pageData) => (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t(TenantsKeys.Manage.ColId)}</TableHead>
                <TableHead>{t(TenantsKeys.Manage.ColName)}</TableHead>
                <TableHead>{t(TenantsKeys.Manage.ColSlug)}</TableHead>
                <TableHead>{t(TenantsKeys.Manage.ColStatus)}</TableHead>
                <TableHead>{t(TenantsKeys.Manage.ColHosts)}</TableHead>
                <TableHead>{t(TenantsKeys.Manage.ColEdition)}</TableHead>
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
                    <div className="flex flex-wrap gap-2">
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => router.get(routes.tenants.views.edit(tenant.id))}
                      >
                        {t(TenantsKeys.Manage.EditButton)}
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => router.get(routes.tenants.views.features(tenant.id))}
                      >
                        {t(TenantsKeys.Manage.FeaturesButton)}
                      </Button>
                      <Button
                        variant="danger"
                        size="sm"
                        onClick={() => setDeleteTarget({ id: tenant.id, name: tenant.name })}
                      >
                        {t(TenantsKeys.Manage.DeleteButton)}
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
            <DialogTitle>{t(TenantsKeys.Manage.DeleteDialog.Title)}</DialogTitle>
            <DialogDescription>
              {t(TenantsKeys.Manage.DeleteDialog.Confirm, { name: deleteTarget?.name ?? '' })}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="secondary" onClick={() => setDeleteTarget(null)}>
              {t(TenantsKeys.Manage.CancelButton)}
            </Button>
            <Button variant="danger" onClick={handleDelete}>
              {t(TenantsKeys.Manage.DeleteDialog.DeleteButton)}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
