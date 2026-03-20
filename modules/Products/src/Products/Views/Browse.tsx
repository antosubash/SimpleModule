import { Card, CardContent, PageHeader } from '@simplemodule/ui';
import type { Product } from '../types';

export default function Browse({ products }: { products: Product[] }) {
  return (
    <div className="mx-auto max-w-4xl space-y-6">
      <PageHeader className="mb-0" title="Products" description="Browse the product catalog." />
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
