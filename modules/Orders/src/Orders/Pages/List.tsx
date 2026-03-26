import { router } from '@inertiajs/react';
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
import type { Order } from '../types';

interface Props {
  orders: Order[];
}

export default function List({ orders }: Props) {
  const [deleteId, setDeleteId] = useState<number | null>(null);

  function handleDelete() {
    if (deleteId === null) return;
    router.delete(`/orders/${deleteId}`);
    setDeleteId(null);
  }

  return (
    <>
      <DataGridPage
        title="Orders"
        description={`${orders.length} total orders`}
        actions={<Button onClick={() => router.get('/orders/create')}>Create Order</Button>}
        data={orders}
        emptyTitle="No orders yet"
        emptyDescription="Get started by creating your first order."
      >
        {(pageData) => (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>ID</TableHead>
                <TableHead>User</TableHead>
                <TableHead>Items</TableHead>
                <TableHead>Total</TableHead>
                <TableHead>Created</TableHead>
                <TableHead />
              </TableRow>
            </TableHeader>
            <TableBody>
              {pageData.map((order) => (
                <TableRow key={order.id}>
                  <TableCell className="font-medium">#{order.id}</TableCell>
                  <TableCell className="text-text-secondary">{order.userId}</TableCell>
                  <TableCell>
                    <Badge variant="info">
                      {order.items.length} item{order.items.length !== 1 ? 's' : ''}
                    </Badge>
                  </TableCell>
                  <TableCell className="font-medium">${order.total.toFixed(2)}</TableCell>
                  <TableCell className="text-sm text-text-muted">
                    {new Date(order.createdAt).toLocaleDateString()}
                  </TableCell>
                  <TableCell>
                    <div className="flex gap-3">
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => router.get(`/orders/${order.id}/edit`)}
                      >
                        Edit
                      </Button>
                      <Button variant="danger" size="sm" onClick={() => setDeleteId(order.id)}>
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

      <Dialog open={deleteId !== null} onOpenChange={(open) => !open && setDeleteId(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete Order</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete order #{deleteId}? This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="secondary" onClick={() => setDeleteId(null)}>
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
