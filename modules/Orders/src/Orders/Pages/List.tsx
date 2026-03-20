import { router } from '@inertiajs/react';
import {
  Badge,
  Button,
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
    <div>
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1 className="text-2xl font-extrabold tracking-tight">Orders</h1>
          <p className="text-text-muted text-sm mt-1">{orders.length} total orders</p>
        </div>
        <Button onClick={() => router.get('/orders/create')}>Create Order</Button>
      </div>

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
          {orders.map((order) => (
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
          {orders.length === 0 && (
            <TableRow>
              <TableCell colSpan={6} className="py-8 text-center text-text-muted">
                No orders yet. Create your first order!
              </TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>

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
    </div>
  );
}
