import { router } from '@inertiajs/react';
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
import type { Product } from '../types';

interface Props {
  products: Product[];
}

export default function Manage({ products }: Props) {
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
        title="Manage Products"
        description={`${products.length} total products`}
        actions={<Button onClick={() => router.get('/products/create')}>Create Product</Button>}
        data={products}
        emptyTitle="No products yet"
        emptyDescription="Get started by creating your first product."
      >
        {(pageData) => (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>ID</TableHead>
                <TableHead>Name</TableHead>
                <TableHead>Price</TableHead>
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
                        Edit
                      </Button>
                      <Button
                        variant="danger"
                        size="sm"
                        onClick={() => setDeleteTarget({ id: product.id, name: product.name })}
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
      </DataGridPage>

      <Dialog open={deleteTarget !== null} onOpenChange={(open) => !open && setDeleteTarget(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete Product</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete &ldquo;{deleteTarget?.name}&rdquo;? This action cannot
              be undone.
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
    </>
  );
}
