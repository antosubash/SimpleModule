import { Card, CardContent, PageShell } from '@simplemodule/ui';
import type { Product } from '../types';

export default function Browse({ products }: { products: Product[] }) {
  return (
    <PageShell title="Products" description="Browse the product catalog.">
      <div className="space-y-3">
        {products.map((p) => (
          <Card key={p.id} data-testid="product-card">
            <CardContent className="flex justify-between items-center">
              <span className="font-medium">{p.name}</span>
              <span className="text-text-muted">${p.price.toFixed(2)}</span>
            </CardContent>
          </Card>
        ))}
      </div>
    </PageShell>
  );
}
