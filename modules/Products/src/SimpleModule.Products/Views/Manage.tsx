import { router } from '@inertiajs/react';
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
import { ProductsKeys } from '../Locales/keys';
import type { Product } from '../types';

interface Props {
  products: Product[];
}

export default function Manage({ products }: Props) {
  const { t } = useTranslation('Products');
  const [deleteTarget, setDeleteTarget] = useState<{
    id: number;
    name: string;
  } | null>(null);

  function handleDelete() {
    if (!deleteTarget) return;
    router.delete(`/products/${deleteTarget.id}`);
    setDeleteTarget(null);
  }

  return (
    <>
      <DataGridPage
        title={t(ProductsKeys.Manage.Title)}
        description={`${products.length} total products`}
        actions={
          <Button onClick={() => router.get('/products/create')}>
            {t(ProductsKeys.Manage.CreateButton)}
          </Button>
        }
        data={products}
        emptyTitle={t(ProductsKeys.Manage.EmptyTitle)}
        emptyDescription={t(ProductsKeys.Manage.EmptyDescription)}
      >
        {(pageData) => (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t(ProductsKeys.Manage.ColId)}</TableHead>
                <TableHead>{t(ProductsKeys.Manage.ColName)}</TableHead>
                <TableHead>{t(ProductsKeys.Manage.ColPrice)}</TableHead>
                <TableHead />
              </TableRow>
            </TableHeader>
            <TableBody>
              {pageData.map((product) => (
                <TableRow key={product.id}>
                  <TableCell className="text-text-muted">#{product.id}</TableCell>
                  <TableCell className="font-medium text-text">{product.name}</TableCell>
                  <TableCell>${product.price.toFixed(2)}</TableCell>
                  <TableCell>
                    <div className="flex gap-3">
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => router.get(`/products/${product.id}/edit`)}
                      >
                        {t(ProductsKeys.Manage.EditButton)}
                      </Button>
                      <Button
                        variant="danger"
                        size="sm"
                        onClick={() => setDeleteTarget({ id: product.id, name: product.name })}
                      >
                        {t(ProductsKeys.Manage.DeleteButton)}
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
            <DialogTitle>{t(ProductsKeys.Manage.DeleteDialog.Title)}</DialogTitle>
            <DialogDescription>
              {t(ProductsKeys.Manage.DeleteDialog.Confirm, { name: deleteTarget?.name })}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="secondary" onClick={() => setDeleteTarget(null)}>
              {t(ProductsKeys.Manage.CancelButton)}
            </Button>
            <Button variant="danger" onClick={handleDelete}>
              {t(ProductsKeys.Manage.DeleteDialog.DeleteButton)}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
