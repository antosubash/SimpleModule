interface Product {
  id: number;
  name: string;
  price: number;
}

export default function Browse({ products }: { products: Product[] }) {
  return (
    <div className="max-w-4xl mx-auto p-8">
      <h1 className="text-3xl font-bold mb-6">Products</h1>
      <ul className="space-y-3">
        {products.map((p) => (
          <li
            key={p.id}
            className="flex justify-between items-center p-4 rounded-lg border border-gray-200 dark:border-gray-700"
          >
            <span className="font-medium">{p.name}</span>
            <span className="text-gray-600 dark:text-gray-400">${p.price.toFixed(2)}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}
