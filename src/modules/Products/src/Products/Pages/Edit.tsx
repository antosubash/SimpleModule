import { router } from '@inertiajs/react';

interface Product {
  id: number;
  name: string;
  price: number;
}

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
        <a href="/products/manage" className="text-text-muted hover:text-text transition-colors no-underline">
          <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path d="M15 19l-7-7 7-7"/></svg>
        </a>
        <h1 className="text-2xl font-extrabold tracking-tight" style={{ fontFamily: "'Sora', sans-serif" }}>
          <span className="gradient-text">Edit Product</span>
        </h1>
      </div>
      <p className="text-text-muted text-sm ml-7 mb-6">Product #{product.id}</p>

      <form onSubmit={handleSubmit} className="glass-card p-6 mb-6">
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1">Name</label>
            <input type="text" name="name" defaultValue={product.name} required />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">Price</label>
            <input type="number" name="price" defaultValue={product.price} required min="0.01" step="0.01" />
          </div>
          <button type="submit" className="btn-primary">Save</button>
        </div>
      </form>

      <div className="glass-card p-6">
        <h2 className="text-lg font-semibold mb-3">Danger Zone</h2>
        <p className="text-sm text-text-muted mb-3">Permanently delete this product. This action cannot be undone.</p>
        <button onClick={handleDelete} className="btn-danger">Delete Product</button>
      </div>
    </div>
  );
}
