import { router } from '@inertiajs/react';
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
  Container,
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
import type { OrderItem } from '../types';

interface Product {
  id: number;
  name: string;
  price: number;
}

interface Props {
  products: Product[];
}

export default function Create({ products }: Props) {
  const [userId, setUserId] = useState('');
  const [items, setItems] = useState<OrderItem[]>([
    { productId: products[0]?.id ?? 0, quantity: 1 },
  ]);

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
    router.post('/orders', { userId, items: items as unknown as string });
  }

  return (
    <Container className="space-y-6">
      <Breadcrumb>
        <BreadcrumbList>
          <BreadcrumbItem>
            <BreadcrumbLink href="/orders">Orders</BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbPage>Create Order</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>
      <h1 className="text-2xl font-bold tracking-tight">Create Order</h1>

      <Card>
        <CardContent className="p-6">
          <form onSubmit={handleSubmit}>
            <FieldGroup>
              <Field>
                <Label htmlFor="userId">User ID</Label>
                <Input
                  id="userId"
                  value={userId}
                  onChange={(e) => setUserId(e.target.value)}
                  required
                  placeholder="Enter user ID"
                />
              </Field>

              <div>
                <div className="flex justify-between items-center mb-2">
                  <Label>Items</Label>
                  <Button type="button" variant="secondary" size="sm" onClick={addItem}>
                    + Add Item
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
                        aria-label={`Quantity for item ${index + 1}`}
                      />
                      {items.length > 1 && (
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          onClick={() => removeItem(index)}
                          aria-label={`Remove item ${index + 1}`}
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
                  <span>Estimated Total</span>
                  <span>${getTotal().toFixed(2)}</span>
                </div>
              </div>

              <Button type="submit">Create Order</Button>
            </FieldGroup>
          </form>
        </CardContent>
      </Card>
    </Container>
  );
}
