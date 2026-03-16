import { router } from '@inertiajs/react';
import {
  Badge,
  Button,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';

interface OrderItem {
  productId: number;
  quantity: number;
}

interface Order {
  id: number;
  userId: string;
  items: OrderItem[];
  total: number;
  createdAt: string;
}

interface Props {
  orders: Order[];
}

export default function List({ orders }: Props) {
  function handleDelete(id: number) {
    if (!confirm(`Delete order #${id}?`)) return;
    router.delete(`/orders/${id}`);
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
                  <Button variant="danger" size="sm" onClick={() => handleDelete(order.id)}>
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
    </div>
  );
}
