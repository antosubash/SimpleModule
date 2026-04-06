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
import { useRef, useState } from 'react';
import { FileStorageKeys } from '@/Locales/keys';
import type { StoredFile } from '@/types';

interface Props {
  files: StoredFile[];
  folders: string[];
  currentFolder: string | null;
  parentFolder: string | null;
}

function formatSize(bytes: number): string {
  if (bytes === 0) return '0 B';
  const units = ['B', 'KB', 'MB', 'GB'];
  const i = Math.floor(Math.log(bytes) / Math.log(1024));
  return `${(bytes / 1024 ** i).toFixed(i === 0 ? 0 : 1)} ${units[i]}`;
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
}

function folderName(path: string): string {
  const parts = path.split('/');
  return parts[parts.length - 1];
}

function breadcrumbs(
  folder: string | null,
  rootLabel: string,
): { label: string; path: string | null }[] {
  const crumbs: { label: string; path: string | null }[] = [{ label: rootLabel, path: null }];
  if (!folder) return crumbs;
  const parts = folder.split('/');
  for (let i = 0; i < parts.length; i++) {
    crumbs.push({
      label: parts[i],
      path: parts.slice(0, i + 1).join('/'),
    });
  }
  return crumbs;
}

export default function Browse({ files, folders, currentFolder, parentFolder }: Props) {
  const { t } = useTranslation('FileStorage');
  const [deleteTarget, setDeleteTarget] = useState<{ id: number; name: string } | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  async function handleDelete() {
    if (!deleteTarget) return;
    await fetch(routes.fileStorage.api.delete(deleteTarget.id), { method: 'DELETE' });
    setDeleteTarget(null);
    router.reload();
  }

  async function handleUpload(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;

    const formData = new FormData();
    formData.append('file', file);
    if (currentFolder) {
      formData.append('folder', currentFolder);
    }

    await fetch(routes.fileStorage.api.upload(), { method: 'POST', body: formData });
    router.reload();

    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  }

  const crumbs = breadcrumbs(currentFolder, t(FileStorageKeys.Browse.BreadcrumbRoot));
  const hasContent = folders.length > 0 || files.length > 0;

  return (
    <>
      <DataGridPage
        title={
          <nav className="flex items-center gap-1 text-sm">
            {crumbs.map((crumb, i) => (
              <span key={crumb.path ?? 'root'} className="flex items-center gap-1">
                {i > 0 && <span className="text-text-muted">/</span>}
                {i < crumbs.length - 1 ? (
                  <button
                    type="button"
                    className="text-primary hover:underline"
                    onClick={() =>
                      router.get(
                        routes.fileStorage.views.browse(),
                        crumb.path ? { folder: crumb.path } : {},
                      )
                    }
                  >
                    {crumb.label}
                  </button>
                ) : (
                  <span className="font-medium">{crumb.label}</span>
                )}
              </span>
            ))}
          </nav>
        }
        description={`${files.length} file${files.length !== 1 ? 's' : ''}, ${folders.length} folder${folders.length !== 1 ? 's' : ''}`}
        actions={
          <>
            <input ref={fileInputRef} type="file" className="hidden" onChange={handleUpload} />
            <Button onClick={() => fileInputRef.current?.click()}>
              {t(FileStorageKeys.Browse.UploadButton)}
            </Button>
          </>
        }
        data={hasContent ? files : []}
        emptyTitle={t(FileStorageKeys.Browse.EmptyTitle)}
        emptyDescription={t(FileStorageKeys.Browse.EmptyDescription)}
      >
        {() => (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t(FileStorageKeys.Browse.ColName)}</TableHead>
                <TableHead>{t(FileStorageKeys.Browse.ColSize)}</TableHead>
                <TableHead>{t(FileStorageKeys.Browse.ColType)}</TableHead>
                <TableHead>{t(FileStorageKeys.Browse.ColDate)}</TableHead>
                <TableHead />
              </TableRow>
            </TableHeader>
            <TableBody>
              {parentFolder !== undefined && currentFolder && (
                <TableRow
                  className="cursor-pointer hover:bg-muted/50"
                  onClick={() =>
                    router.get(
                      routes.fileStorage.views.browse(),
                      parentFolder ? { folder: parentFolder } : {},
                    )
                  }
                >
                  <TableCell className="font-medium" colSpan={5}>
                    ..
                  </TableCell>
                </TableRow>
              )}
              {folders.map((f) => (
                <TableRow
                  key={f}
                  className="cursor-pointer hover:bg-muted/50"
                  onClick={() => router.get(routes.fileStorage.views.browse(), { folder: f })}
                >
                  <TableCell className="font-medium">
                    <span className="mr-2">📁</span>
                    {folderName(f)}
                  </TableCell>
                  <TableCell className="text-text-muted">&mdash;</TableCell>
                  <TableCell className="text-text-muted">
                    {t(FileStorageKeys.Browse.FolderType)}
                  </TableCell>
                  <TableCell className="text-text-muted">&mdash;</TableCell>
                  <TableCell />
                </TableRow>
              ))}
              {files.map((file) => (
                <TableRow key={file.id}>
                  <TableCell className="font-medium">{file.fileName}</TableCell>
                  <TableCell className="text-text-muted">{formatSize(file.size)}</TableCell>
                  <TableCell className="text-text-muted">{file.contentType}</TableCell>
                  <TableCell className="text-text-muted">{formatDate(file.createdAt)}</TableCell>
                  <TableCell>
                    <div className="flex gap-3">
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() =>
                          window.open(routes.fileStorage.api.download(file.id), '_blank')
                        }
                      >
                        {t(FileStorageKeys.Browse.DownloadButton)}
                      </Button>
                      <Button
                        variant="danger"
                        size="sm"
                        onClick={() => setDeleteTarget({ id: file.id, name: file.fileName })}
                      >
                        {t(FileStorageKeys.Browse.DeleteButton)}
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
            <DialogTitle>{t(FileStorageKeys.Browse.DeleteDialog.Title)}</DialogTitle>
            <DialogDescription>
              {t(FileStorageKeys.Browse.DeleteDialog.Description, {
                name: deleteTarget?.name ?? '',
              })}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="secondary" onClick={() => setDeleteTarget(null)}>
              {t(FileStorageKeys.Browse.DeleteDialog.CancelButton)}
            </Button>
            <Button variant="danger" onClick={handleDelete}>
              {t(FileStorageKeys.Browse.DeleteDialog.ConfirmButton)}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
