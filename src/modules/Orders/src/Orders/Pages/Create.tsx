import { router } from '@inertiajs/react';
import { useState } from 'react';

interface Product {
  id: number;
  name: string;
  price: number;
}

interface Props {
  products: Product[];
}

interface ItemRow {
  productId: number;
  quantity: number;
}

export default function Create({ products }: Props) {
  const [userId, setUserId] = useState('');
  const [items, setItems] = useState<ItemRow[]>([{ productId: products[0]?.id ?? 0, quantity: 1 }]);

  function addItem() {
    setItems([...items, { productId: products[0]?.id ?? 0, quantity: 1 }]);
  }

  function removeItem(index: number) {
    setItems(items.filter((_, i) => i !== index));
  }

  function updateItem(index: number, field: keyof ItemRow, value: number) {
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
        <a href="/orders" className="text-text-muted hover:text-text transition-colors no-underline">
          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path d="M15 19l-7-7 7-7"/></svg>
        </a>
        <h1 className="text-2xl font-extrabold tracking-tight" style={{ fontFamily: "'Sora', sans-serif" }}>
          <span className="gradient-text">Create Order</span>
        </h1>
      </div>
      <p className="text-text-muted text-sm ml-7 mb-6">Add a new order</p>

      <form onSubmit={handleSubmit} className="glass-card p-6">
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1">User ID</label>
            <input
              type="text"
              value={userId}
              onChange={(e) => setUserId(e.target.value)}
              required
              placeholder="Enter user ID"
            />
          </div>

          <div>
            <div className="flex justify-between items-center mb-2">
              <label className="block text-sm font-medium">Items</label>
              <button type="button" onClick={addItem} className="btn-secondary" style={{ padding: '0.25rem 0.75rem', fontSize: '0.8rem' }}>
                + Add Item
              </button>
            </div>
            <div className="space-y-2">
              {items.map((item, index) => (
                <div key={index} className="flex gap-2 items-center">
                  <select
                    value={item.productId}
                    onChange={(e) => updateItem(index, 'productId', Number(e.target.value))}
                    className="flex-1"
                  >
                    {products.map((p) => (
                      <option key={p.id} value={p.id}>
                        {p.name} (${p.price.toFixed(2)})
                      </option>
                    ))}
                  </select>
                  <input
                    type="number"
                    value={item.quantity}
                    onChange={(e) => updateItem(index, 'quantity', Math.max(1, Number(e.target.value)))}
                    min="1"
                    style={{ width: '5rem' }}
                  />
                  {items.length > 1 && (
                    <button
                      type="button"
                      onClick={() => removeItem(index)}
                      className="text-danger hover:text-danger-hover bg-transparent border-none cursor-pointer text-lg"
                      title="Remove item"
                    >
                      &times;
                    </button>
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

          <button type="submit" className="btn-primary">Create Order</button>
        </div>
      </form>
    </div>
  );
}
