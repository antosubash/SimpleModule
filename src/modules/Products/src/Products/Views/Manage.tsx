import { router } from '@inertiajs/react';

interface Product {
  id: number;
  name: string;
  price: number;
}

interface Props {
  products: Product[];
}

export default function Manage({ products }: Props) {
  function handleDelete(id: number, name: string) {
    if (!confirm(`Delete product "${name}"?`)) return;
    router.delete(`/products/${id}`);
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1
            className="text-2xl font-extrabold tracking-tight"
            style={{ fontFamily: "'Sora', sans-serif" }}
          >
            <span className="gradient-text">Manage Products</span>
          </h1>
          <p className="text-text-muted text-sm mt-1">{products.length} total products</p>
        </div>
        <button onClick={() => router.get('/products/create')} className="btn-primary">
          Create Product
        </button>
      </div>

      <div className="glass-card overflow-x-auto">
        <table className="w-full text-left">
          <thead>
            <tr>
              <th className="px-4 py-3">ID</th>
              <th className="px-4 py-3">Name</th>
              <th className="px-4 py-3">Price</th>
              <th className="px-4 py-3"></th>
            </tr>
          </thead>
          <tbody>
            {products.map((product) => (
              <tr key={product.id} className="hover:bg-surface-raised transition-colors">
                <td className="px-4 py-3 text-text-muted">#{product.id}</td>
                <td className="px-4 py-3 font-medium text-text">{product.name}</td>
                <td className="px-4 py-3 text-text">${product.price.toFixed(2)}</td>
                <td className="px-4 py-3">
                  <div className="flex gap-3">
                    <button
                      onClick={() => router.get(`/products/${product.id}/edit`)}
                      className="text-primary hover:text-primary-hover text-sm font-medium bg-transparent border-none cursor-pointer"
                    >
                      Edit
                    </button>
                    <button
                      onClick={() => handleDelete(product.id, product.name)}
                      className="text-danger hover:text-danger-hover text-sm font-medium bg-transparent border-none cursor-pointer"
                    >
                      Delete
                    </button>
                  </div>
                </td>
              </tr>
            ))}
            {products.length === 0 && (
              <tr>
                <td colSpan={4} className="px-4 py-8 text-center text-text-muted">
                  No products yet. Create your first product!
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
