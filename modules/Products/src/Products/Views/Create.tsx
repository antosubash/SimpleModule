import { router } from '@inertiajs/react';
import { Button, Card, CardContent, Field, FieldGroup, Input, Label } from '@simplemodule/ui';

export default function Create() {
  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post('/products', formData);
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
        <h1 className="text-2xl font-extrabold tracking-tight">Create Product</h1>
      </div>
      <p className="text-text-muted text-sm ml-7 mb-6">Add a new product</p>

      <Card>
        <CardContent className="p-6">
          <form onSubmit={handleSubmit}>
            <FieldGroup>
              <Field>
                <Label htmlFor="name">Name</Label>
                <Input id="name" name="name" required placeholder="Product name" />
              </Field>
              <Field>
                <Label htmlFor="price">Price</Label>
                <Input
                  id="price"
                  name="price"
                  type="number"
                  required
                  min={0.01}
                  step={0.01}
                  placeholder="0.00"
                />
              </Field>
              <Button type="submit">Create</Button>
            </FieldGroup>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
