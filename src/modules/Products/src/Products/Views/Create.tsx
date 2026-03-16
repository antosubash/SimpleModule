import { router } from '@inertiajs/react';
import { Button, Input, Label, Card, CardContent } from '@simplemodule/ui';

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
        </a>
        <h1
          className="text-2xl font-extrabold tracking-tight"
          style={{ fontFamily: "'Sora', sans-serif" }}
        >
          <span className="gradient-text">Create Product</span>
        </h1>
      </div>
      <p className="text-text-muted text-sm ml-7 mb-6">Add a new product</p>

      <Card className="glass-card">
        <CardContent className="p-6">
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <Label htmlFor="name">Name</Label>
              <Input id="name" name="name" required placeholder="Product name" />
            </div>
            <div>
              <Label htmlFor="price">Price</Label>
              <Input id="price" name="price" type="number" required min={0.01} step={0.01} placeholder="0.00" />
            </div>
            <Button type="submit">Create</Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
