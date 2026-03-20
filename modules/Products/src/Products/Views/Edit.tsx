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
  CardHeader,
  CardTitle,
  Input,
  Label,
} from '@simplemodule/ui';
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
      <Breadcrumb className="mb-4">
        <BreadcrumbList>
          <BreadcrumbItem>
            <BreadcrumbLink href="/products/manage">Products</BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbPage>Edit Product</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>
      <h1 className="text-2xl font-bold tracking-tight mb-6">Edit Product</h1>

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
