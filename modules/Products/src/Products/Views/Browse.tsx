import { Card, CardContent } from '@simplemodule/ui';
import type { Product } from '../types';

export default function Browse({ products }: { products: Product[] }) {
  return (
    <div className="max-w-4xl mx-auto">
      <h1 className="text-2xl font-extrabold tracking-tight mb-6">Products</h1>
      <div className="space-y-3">
        {products.map((p) => (
          <Card key={p.id}>
            <CardContent className="flex justify-between items-center">
              <span className="font-medium">{p.name}</span>
              <span className="text-text-muted">${p.price.toFixed(2)}</span>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}
