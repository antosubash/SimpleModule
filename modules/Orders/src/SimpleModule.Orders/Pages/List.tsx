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
import { OrdersKeys } from '../Locales/keys';
import type { Order } from '../types';

interface Props {
  orders: Order[];
}

export default function List({ orders }: Props) {
  const { t } = useTranslation('Orders');
  const [deleteId, setDeleteId] = useState<number | null>(null);

  function handleDelete() {
    if (deleteId === null) return;
    router.delete(`/orders/${deleteId}`);
    setDeleteId(null);
  }

  return (
    <>
      <DataGridPage
        title={t(OrdersKeys.List.Title)}
        description={t(OrdersKeys.List.Description).replace('{count}', String(orders.length))}
        actions={
          <Button onClick={() => router.get('/orders/create')}>
            {t(OrdersKeys.List.CreateButton)}
          </Button>
        }
        data={orders}
        emptyTitle={t(OrdersKeys.List.EmptyTitle)}
        emptyDescription={t(OrdersKeys.List.EmptyDescription)}
      >
        {(pageData) => (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t(OrdersKeys.List.ColId)}</TableHead>
                <TableHead>{t(OrdersKeys.List.ColUser)}</TableHead>
                <TableHead>{t(OrdersKeys.List.ColItems)}</TableHead>
                <TableHead>{t(OrdersKeys.List.ColTotal)}</TableHead>
                <TableHead>{t(OrdersKeys.List.ColCreated)}</TableHead>
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
                        {t(OrdersKeys.List.EditButton)}
                      </Button>
                      <Button variant="danger" size="sm" onClick={() => setDeleteId(order.id)}>
                        {t(OrdersKeys.List.DeleteButton)}
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
            <DialogTitle>{t(OrdersKeys.List.DeleteDialog.Title)}</DialogTitle>
            <DialogDescription>
              {t(OrdersKeys.List.DeleteDialog.Confirm).replace('{id}', String(deleteId))}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="secondary" onClick={() => setDeleteId(null)}>
              {t(OrdersKeys.List.CancelButton)}
            </Button>
            <Button variant="danger" onClick={handleDelete}>
              {t(OrdersKeys.List.DeleteButton)}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
