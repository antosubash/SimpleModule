import { router } from '@inertiajs/react';
import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Container,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Field,
  FieldGroup,
  Input,
  Label,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@simplemodule/ui';
import { useState } from 'react';
import { OrdersKeys } from '../Locales/keys';
import type { Order, OrderItem } from '../types';

interface Product {
  id: number;
  name: string;
  price: number;
}

interface Props {
  order: Order;
  products: Product[];
}

export default function Edit({ order, products }: Props) {
  const { t } = useTranslation('Orders');
  const [userId, setUserId] = useState(order.userId);
  const [items, setItems] = useState<OrderItem[]>(
    order.items.map((i) => ({ productId: i.productId, quantity: i.quantity })),
  );
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);

  function addItem() {
    setItems([...items, { productId: products[0]?.id ?? 0, quantity: 1 }]);
  }

  function removeItem(index: number) {
    setItems(items.filter((_, i) => i !== index));
  }

  function updateItem(index: number, field: keyof OrderItem, value: number) {
    const updated = [...items];
    updated[index] = { ...updated[index], [field]: value };
    setItems(updated);
  }

  function getTotal() {
    return items.reduce((sum, item) => {
      const product = products.find((p) => p.id === item.productId);
      return sum + (product?.price ?? 0) * item.quantity;
    }, 0);
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    router.post(`/orders/${order.id}`, { userId, items: items as unknown as string });
  }

  function handleDelete() {
    router.delete(`/orders/${order.id}`);
    setShowDeleteDialog(false);
  }

  return (
    <Container className="space-y-6">
      <Breadcrumb>
        <BreadcrumbList>
          <BreadcrumbItem>
            <BreadcrumbLink href="/orders">{t(OrdersKeys.List.Title)}</BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbPage>{t(OrdersKeys.Edit.Breadcrumb)}</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>
      <h1 className="text-2xl font-bold tracking-tight">
        {t(OrdersKeys.Edit.Title).replace('{id}', String(order.id))}
      </h1>

      <Card>
        <CardContent className="p-6">
          <form onSubmit={handleSubmit}>
            <FieldGroup>
              <Field>
                <Label htmlFor="userId">{t(OrdersKeys.Edit.UserIdLabel)}</Label>
                <Input
                  id="userId"
                  value={userId}
                  onChange={(e) => setUserId(e.target.value)}
                  required
                />
              </Field>

              <div>
                <div className="flex justify-between items-center mb-2">
                  <Label>{t(OrdersKeys.Edit.ItemsLabel)}</Label>
                  <Button type="button" variant="secondary" size="sm" onClick={addItem}>
                    {t(OrdersKeys.Edit.AddItemButton)}
                  </Button>
                </div>
                <div className="space-y-2">
                  {items.map((item, index) => (
                    // biome-ignore lint/suspicious/noArrayIndexKey: line items have no stable ID
                    <div key={index} className="flex gap-2 items-center">
                      <Select
                        value={String(item.productId)}
                        onValueChange={(value) => updateItem(index, 'productId', Number(value))}
                      >
                        <SelectTrigger className="flex-1">
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          {products.map((p) => (
                            <SelectItem key={p.id} value={String(p.id)}>
                              {p.name} (${p.price.toFixed(2)})
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                      <Input
                        type="number"
                        value={item.quantity}
                        onChange={(e) =>
                          updateItem(index, 'quantity', Math.max(1, Number(e.target.value)))
                        }
                        min="1"
                        className="w-20"
                        aria-label={t(OrdersKeys.Edit.QuantityLabel).replace(
                          '{index}',
                          String(index + 1),
                        )}
                      />
                      {items.length > 1 && (
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          onClick={() => removeItem(index)}
                          aria-label={t(OrdersKeys.Edit.RemoveButton).replace(
                            '{index}',
                            String(index + 1),
                          )}
                        >
                          &times;
                        </Button>
                      )}
                    </div>
                  ))}
                </div>
              </div>

              <div className="pt-2 border-t border-border" data-testid="estimated-total">
                <div className="flex justify-between items-center text-lg font-semibold">
                  <span>{t(OrdersKeys.Edit.TotalLabel)}</span>
                  <span>${getTotal().toFixed(2)}</span>
                </div>
              </div>

              <Button type="submit">{t(OrdersKeys.Edit.SaveButton)}</Button>
            </FieldGroup>
          </form>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t(OrdersKeys.Edit.DangerZone)}</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-text-muted mb-3">{t(OrdersKeys.Edit.DangerZoneDescription)}</p>
          <Button variant="danger" onClick={() => setShowDeleteDialog(true)}>
            {t(OrdersKeys.Edit.DeleteButton)}
          </Button>
        </CardContent>
      </Card>

      <Dialog open={showDeleteDialog} onOpenChange={setShowDeleteDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t(OrdersKeys.Edit.DeleteDialog.Title)}</DialogTitle>
            <DialogDescription>
              {t(OrdersKeys.Edit.DeleteDialog.Confirm).replace('{id}', String(order.id))}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="secondary" onClick={() => setShowDeleteDialog(false)}>
              {t(OrdersKeys.Edit.CancelButton)}
            </Button>
            <Button variant="danger" onClick={handleDelete}>
              {t(OrdersKeys.Edit.DeleteButton)}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </Container>
  );
}
