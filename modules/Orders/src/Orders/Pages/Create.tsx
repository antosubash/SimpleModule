import { router } from '@inertiajs/react';
import {
  Button,
  Card,
  CardContent,
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
    router.post('/orders', { userId, items });
  }

  return (
    <div className="max-w-2xl">
      <div className="flex items-center gap-3 mb-1">
        <a
          href="/orders"
          className="text-text-muted hover:text-text transition-colors no-underline"
          aria-label="Back to orders"
        >
          <svg
            className="w-4 h-4"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            viewBox="0 0 24 24"
          >
            <path d="M15 19l-7-7 7-7" />
          </svg>
        </a>
        <h1 className="text-2xl font-extrabold tracking-tight">Create Order</h1>
      </div>
      <p className="text-text-muted text-sm ml-7 mb-6">Add a new order</p>

      <Card>
        <CardContent className="p-6">
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <Label htmlFor="userId">User ID</Label>
              <Input
                id="userId"
                value={userId}
                onChange={(e) => setUserId(e.target.value)}
                required
                placeholder="Enter user ID"
              />
            </div>

            <div>
              <div className="flex justify-between items-center mb-2">
                <Label>Items</Label>
                <Button type="button" variant="secondary" size="sm" onClick={addItem}>
                  + Add Item
                </Button>
              </div>
              <div className="space-y-2">
                {items.map((item, index) => (
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

            <div className="pt-2 border-t border-border">
              <div className="flex justify-between items-center text-lg font-semibold">
                <span>Estimated Total</span>
                <span>${getTotal().toFixed(2)}</span>
              </div>
            </div>

            <Button type="submit">Create Order</Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
