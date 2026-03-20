import { router } from '@inertiajs/react';
import {
  Button,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';
import type { Product } from '../types';

interface Props {
  products: Product[];
}

export default function Manage({ products }: Props) {
  function handleDelete(id: number, name: string) {
    if (!confirm(`Delete product "${name}"?`)) return;
    router.delete(`/products/${id}`);
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <div className="space-y-1">
          <h1 className="text-2xl font-bold tracking-tight">Manage Products</h1>
          <p className="text-sm text-muted-foreground">{products.length} total products</p>
        </div>
        <Button onClick={() => router.get('/products/create')}>Create Product</Button>
      </div>

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
          {products.map((product) => (
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
                    onClick={() => handleDelete(product.id, product.name)}
                  >
                    Delete
                  </Button>
                </div>
              </TableCell>
            </TableRow>
          ))}
          {products.length === 0 && (
            <TableRow>
              <TableCell colSpan={4} className="py-8 text-center text-text-muted">
                No products yet. Create your first product!
              </TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
    </div>
  );
}
