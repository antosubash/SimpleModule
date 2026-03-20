import { Card, CardContent } from '@simplemodule/ui';
import type { Product } from '../types';

export default function Browse({ products }: { products: Product[] }) {
  return (
    <div className="max-w-4xl mx-auto">
      <div className="space-y-1 mb-6">
        <h1 className="text-2xl font-bold tracking-tight">Products</h1>
        <p className="text-sm text-muted-foreground">Browse the product catalog.</p>
      </div>
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
