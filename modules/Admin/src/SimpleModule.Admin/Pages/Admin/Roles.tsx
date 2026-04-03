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
import { AdminKeys } from '@/Locales/keys';

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
  const { t } = useTranslation('Admin');
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
        setDeleteError(t(AdminKeys.Roles.DeleteError));
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
        title={t(AdminKeys.Roles.Title)}
        description={t(AdminKeys.Roles.Description)}
        actions={
          <Button onClick={() => router.get('/admin/roles/create')}>
            {t(AdminKeys.Roles.CreateButton)}
          </Button>
        }
        data={roles}
        filterBar={errorAlert}
      >
        {(pageData) => (
          <div className="overflow-x-auto -mx-4 px-4 sm:mx-0 sm:px-0">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t(AdminKeys.Roles.ColName)}</TableHead>
                  <TableHead>{t(AdminKeys.Roles.ColDescription)}</TableHead>
                  <TableHead>{t(AdminKeys.Roles.ColUsers)}</TableHead>
                  <TableHead>{t(AdminKeys.Roles.ColPermissions)}</TableHead>
                  <TableHead>{t(AdminKeys.Roles.ColCreated)}</TableHead>
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
                      <Badge variant="default">{role.permissionCount}</Badge>
                    </TableCell>
                    <TableCell className="text-sm text-text-muted">
                      {new Date(role.createdAt).toLocaleDateString()}
                    </TableCell>
                    <TableCell>
                      <div className="flex flex-wrap gap-2">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => router.get(`/admin/roles/${role.id}/edit`)}
                        >
                          {t(AdminKeys.Roles.EditButton)}
                        </Button>
                        <Button
                          variant="danger"
                          size="sm"
                          onClick={() => setDeleteTarget({ id: role.id, name: role.name })}
                        >
                          {t(AdminKeys.Roles.DeleteButton)}
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        )}
      </DataGridPage>

      <Dialog open={deleteTarget !== null} onOpenChange={(open) => !open && setDeleteTarget(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t(AdminKeys.Roles.DeleteDialog.Title)}</DialogTitle>
            <DialogDescription>
              {t(AdminKeys.Roles.DeleteDialog.Confirm, { name: deleteTarget?.name ?? '' })}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter className="flex flex-wrap gap-2">
            <Button variant="secondary" onClick={() => setDeleteTarget(null)}>
              {t(AdminKeys.Roles.DeleteDialog.CancelButton)}
            </Button>
            <Button variant="danger" onClick={handleDelete}>
              {t(AdminKeys.Roles.DeleteDialog.DeleteButton)}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
