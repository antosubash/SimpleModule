import { router } from '@inertiajs/react';

interface OrderItem {
  productId: number;
  quantity: number;
}

interface Order {
  id: number;
  userId: string;
  items: OrderItem[];
  total: number;
  createdAt: string;
}

interface Props {
  orders: Order[];
}

export default function List({ orders }: Props) {
  function handleDelete(id: number) {
    if (!confirm(`Delete order #${id}?`)) return;
    router.delete(`/orders/${id}`);
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1
            className="text-2xl font-extrabold tracking-tight"
            style={{ fontFamily: "'Sora', sans-serif" }}
          >
            <span className="gradient-text">Orders</span>
          </h1>
          <p className="text-text-muted text-sm mt-1">{orders.length} total orders</p>
        </div>
        <button onClick={() => router.get('/orders/create')} className="btn-primary">
          Create Order
        </button>
      </div>

      <div className="glass-card overflow-x-auto">
        <table className="w-full text-left">
          <thead>
            <tr>
              <th className="px-4 py-3">ID</th>
              <th className="px-4 py-3">User</th>
              <th className="px-4 py-3">Items</th>
              <th className="px-4 py-3">Total</th>
              <th className="px-4 py-3">Created</th>
              <th className="px-4 py-3"></th>
            </tr>
          </thead>
          <tbody>
            {orders.map((order) => (
              <tr key={order.id} className="hover:bg-surface-raised transition-colors">
                <td className="px-4 py-3 font-medium text-text">#{order.id}</td>
                <td className="px-4 py-3 text-text-secondary">{order.userId}</td>
                <td className="px-4 py-3">
                  <span className="badge-info">
                    {order.items.length} item{order.items.length !== 1 ? 's' : ''}
                  </span>
                </td>
                <td className="px-4 py-3 font-medium text-text">${order.total.toFixed(2)}</td>
                <td className="px-4 py-3 text-sm text-text-muted">
                  {new Date(order.createdAt).toLocaleDateString()}
                </td>
                <td className="px-4 py-3">
                  <div className="flex gap-3">
                    <button
                      onClick={() => router.get(`/orders/${order.id}/edit`)}
                      className="text-primary hover:text-primary-hover text-sm font-medium bg-transparent border-none cursor-pointer"
                    >
                      Edit
                    </button>
                    <button
                      onClick={() => handleDelete(order.id)}
                      className="text-danger hover:text-danger-hover text-sm font-medium bg-transparent border-none cursor-pointer"
                    >
                      Delete
                    </button>
                  </div>
                </td>
              </tr>
            ))}
            {orders.length === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-8 text-center text-text-muted">
                  No orders yet. Create your first order!
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
