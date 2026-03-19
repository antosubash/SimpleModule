import { router } from '@inertiajs/react';
import { Button, Card, CardContent, CardHeader, CardTitle, Input, Label } from '@simplemodule/ui';
import type { Product } from '../types';

interface Props {
  product: Product;
}

export default function Edit({ product }: Props) {
  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post(`/products/${product.id}`, formData);
  }

  function handleDelete() {
    if (!confirm(`Delete product "${product.name}"?`)) return;
    router.delete(`/products/${product.id}`);
  }

  return (
    <div className="max-w-xl">
      <div className="flex items-center gap-3 mb-1">
        <a
          href="/products/manage"
          aria-label="Back to manage products"
          className="text-text-muted hover:text-text transition-colors no-underline"
        >
          <svg
            className="w-4 h-4"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <path d="M15 19l-7-7 7-7" />
          </svg>
          <span className="sr-only">Back to manage products</span>
        </a>
        <h1 className="text-2xl font-extrabold tracking-tight">Edit Product</h1>
      </div>
      <p className="text-text-muted text-sm ml-7 mb-6">Product #{product.id}</p>

      <Card className="mb-6">
        <CardContent className="p-6">
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <Label htmlFor="name">Name</Label>
              <Input id="name" name="name" defaultValue={product.name} required />
            </div>
            <div>
              <Label htmlFor="price">Price</Label>
              <Input
                id="price"
                name="price"
                type="number"
                defaultValue={product.price}
                required
                min={0.01}
                step={0.01}
              />
            </div>
            <Button type="submit">Save</Button>
          </form>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Danger Zone</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-text-muted mb-3">
            Permanently delete this product. This action cannot be undone.
          </p>
          <Button variant="danger" onClick={handleDelete}>
            Delete Product
          </Button>
        </CardContent>
      </Card>
    </div>
  );
}
