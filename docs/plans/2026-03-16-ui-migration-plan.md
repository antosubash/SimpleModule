# UI Component Migration Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Migrate all module pages from native HTML + custom CSS to `@simplemodule/ui` components with default styling.

**Architecture:** Replace native HTML elements with `@simplemodule/ui` components (Button, Table, Card, Badge, Input, Label, Select, Checkbox, Alert, Spinner, Separator). Keep all business logic, Inertia routing, and state management identical. Remove custom CSS classes (btn-*, badge-*, glass-card, panel, gradient-text).

**Tech Stack:** React 19, @simplemodule/ui (Radix UI + Tailwind), @inertiajs/react

**Note:** The Radix `Select` component uses string values and `onValueChange` instead of native `<select>` with `onChange` and numeric values. The Radix `Checkbox` supports `name`/`value` for FormData submission. These API differences must be handled carefully.

---

### Task 1: Migrate Orders/List.tsx

**Files:**
- Modify: `src/modules/Orders/src/Orders/Pages/List.tsx`

**Step 1: Rewrite List.tsx using UI components**

Replace the full file with:

```tsx
import { router } from '@inertiajs/react';
import {
  Badge,
  Button,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';

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
          <h1 className="text-2xl font-extrabold tracking-tight">Orders</h1>
          <p className="text-text-muted text-sm mt-1">{orders.length} total orders</p>
        </div>
        <Button onClick={() => router.get('/orders/create')}>Create Order</Button>
      </div>

      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>ID</TableHead>
            <TableHead>User</TableHead>
            <TableHead>Items</TableHead>
            <TableHead>Total</TableHead>
            <TableHead>Created</TableHead>
            <TableHead />
          </TableRow>
        </TableHeader>
        <TableBody>
          {orders.map((order) => (
            <TableRow key={order.id}>
              <TableCell className="font-medium">#{order.id}</TableCell>
              <TableCell className="text-text-secondary">{order.userId}</TableCell>
              <TableCell>
                <Badge variant="info">
                  {order.items.length} item{order.items.length !== 1 ? 's' : ''}
                </Badge>
              </TableCell>
              <TableCell className="font-medium">${order.total.toFixed(2)}</TableCell>
              <TableCell className="text-sm text-text-muted">
                {new Date(order.createdAt).toLocaleDateString()}
              </TableCell>
              <TableCell>
                <div className="flex gap-3">
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => router.get(`/orders/${order.id}/edit`)}
                  >
                    Edit
                  </Button>
                  <Button variant="danger" size="sm" onClick={() => handleDelete(order.id)}>
                    Delete
                  </Button>
                </div>
              </TableCell>
            </TableRow>
          ))}
          {orders.length === 0 && (
            <TableRow>
              <TableCell colSpan={6} className="py-8 text-center text-text-muted">
                No orders yet. Create your first order!
              </TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
    </div>
  );
}
```

**Step 2: Verify**

Run: `npm run check`

**Step 3: Commit**

```bash
git add src/modules/Orders/src/Orders/Pages/List.tsx
git commit -m "feat(orders): migrate List page to @simplemodule/ui components"
```

---

### Task 2: Migrate Orders/Create.tsx

**Files:**
- Modify: `src/modules/Orders/src/Orders/Pages/Create.tsx`

**Step 1: Rewrite Create.tsx using UI components**

Note: Keep native `<select>` for the dynamic product picker since Radix Select doesn't support controlled numeric values as cleanly in dynamic lists. Use UI components for everything else.

```tsx
import { router } from '@inertiajs/react';
import { useState } from 'react';
import { Button, Card, CardContent, Input, Label } from '@simplemodule/ui';

interface Product {
  id: number;
  name: string;
  price: number;
}

interface Props {
  products: Product[];
}

interface ItemRow {
  productId: number;
  quantity: number;
}

export default function Create({ products }: Props) {
  const [userId, setUserId] = useState('');
  const [items, setItems] = useState<ItemRow[]>([{ productId: products[0]?.id ?? 0, quantity: 1 }]);

  function addItem() {
    setItems([...items, { productId: products[0]?.id ?? 0, quantity: 1 }]);
  }

  function removeItem(index: number) {
    setItems(items.filter((_, i) => i !== index));
  }

  function updateItem(index: number, field: keyof ItemRow, value: number) {
    const updated = [...items];
    updated[index] = { ...updated[index], [field]: value };
    setItems(updated);
  }

  function getTotal() {
    return items.reduce((sum, item) => {
      const product = products.find((p) => p.id === item.productId);
      return sum + (product?.price ?? 0) * item.quantity;
    }, 0);
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    router.post('/orders', { userId, items });
  }

  return (
    <div className="max-w-2xl">
      <div className="flex items-center gap-3 mb-1">
        <a href="/orders" className="text-text-muted hover:text-text transition-colors no-underline">
          <svg
            className="w-4 h-4"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            viewBox="0 0 24 24"
          >
            <path d="M15 19l-7-7 7-7" />
          </svg>
        </a>
        <h1 className="text-2xl font-extrabold tracking-tight">Create Order</h1>
      </div>
      <p className="text-text-muted text-sm ml-7 mb-6">Add a new order</p>

      <Card>
        <CardContent className="p-6">
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <Label htmlFor="userId">User ID</Label>
              <Input
                id="userId"
                value={userId}
                onChange={(e) => setUserId(e.target.value)}
                required
                placeholder="Enter user ID"
              />
            </div>

            <div>
              <div className="flex justify-between items-center mb-2">
                <Label>Items</Label>
                <Button type="button" variant="secondary" size="sm" onClick={addItem}>
                  + Add Item
                </Button>
              </div>
              <div className="space-y-2">
                {items.map((item, index) => (
                  <div key={index} className="flex gap-2 items-center">
                    <select
                      value={item.productId}
                      onChange={(e) => updateItem(index, 'productId', Number(e.target.value))}
                      className="flex-1 h-11 rounded-xl border border-border bg-surface px-4 py-3 text-sm text-text transition-all duration-200 outline-none focus:border-primary focus:ring-4 focus:ring-primary-ring"
                    >
                      {products.map((p) => (
                        <option key={p.id} value={p.id}>
                          {p.name} (${p.price.toFixed(2)})
                        </option>
                      ))}
                    </select>
                    <Input
                      type="number"
                      value={item.quantity}
                      onChange={(e) =>
                        updateItem(index, 'quantity', Math.max(1, Number(e.target.value)))
                      }
                      min="1"
                      className="w-20"
                    />
                    {items.length > 1 && (
                      <Button
                        type="button"
                        variant="ghost"
                        size="sm"
                        onClick={() => removeItem(index)}
                      >
                        &times;
                      </Button>
                    )}
                  </div>
                ))}
              </div>
            </div>

            <div className="pt-2 border-t border-border">
              <div className="flex justify-between items-center text-lg font-semibold">
                <span>Estimated Total</span>
                <span>${getTotal().toFixed(2)}</span>
              </div>
            </div>

            <Button type="submit">Create Order</Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
```

**Step 2: Verify**

Run: `npm run check`

**Step 3: Commit**

```bash
git add src/modules/Orders/src/Orders/Pages/Create.tsx
git commit -m "feat(orders): migrate Create page to @simplemodule/ui components"
```

---

### Task 3: Migrate Orders/Edit.tsx

**Files:**
- Modify: `src/modules/Orders/src/Orders/Pages/Edit.tsx`

**Step 1: Rewrite Edit.tsx using UI components**

Same pattern as Create — keep native `<select>` for dynamic product list.

```tsx
import { router } from '@inertiajs/react';
import { useState } from 'react';
import {
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Input,
  Label,
} from '@simplemodule/ui';

interface Product {
  id: number;
  name: string;
  price: number;
}

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
  order: Order;
  products: Product[];
}

interface ItemRow {
  productId: number;
  quantity: number;
}

export default function Edit({ order, products }: Props) {
  const [userId, setUserId] = useState(order.userId);
  const [items, setItems] = useState<ItemRow[]>(
    order.items.map((i) => ({ productId: i.productId, quantity: i.quantity })),
  );

  function addItem() {
    setItems([...items, { productId: products[0]?.id ?? 0, quantity: 1 }]);
  }

  function removeItem(index: number) {
    setItems(items.filter((_, i) => i !== index));
  }

  function updateItem(index: number, field: keyof ItemRow, value: number) {
    const updated = [...items];
    updated[index] = { ...updated[index], [field]: value };
    setItems(updated);
  }

  function getTotal() {
    return items.reduce((sum, item) => {
      const product = products.find((p) => p.id === item.productId);
      return sum + (product?.price ?? 0) * item.quantity;
    }, 0);
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    router.post(`/orders/${order.id}`, { userId, items });
  }

  function handleDelete() {
    if (!confirm(`Delete order #${order.id}?`)) return;
    router.delete(`/orders/${order.id}`);
  }

  return (
    <div className="max-w-2xl">
      <div className="flex items-center gap-3 mb-1">
        <a href="/orders" className="text-text-muted hover:text-text transition-colors no-underline">
          <svg
            className="w-4 h-4"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            viewBox="0 0 24 24"
          >
            <path d="M15 19l-7-7 7-7" />
          </svg>
        </a>
        <h1 className="text-2xl font-extrabold tracking-tight">Edit Order #{order.id}</h1>
      </div>
      <p className="text-text-muted text-sm ml-7 mb-6">
        Created: {new Date(order.createdAt).toLocaleString()} &middot; Current total: $
        {order.total.toFixed(2)}
      </p>

      <Card className="mb-6">
        <CardContent className="p-6">
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <Label htmlFor="userId">User ID</Label>
              <Input
                id="userId"
                value={userId}
                onChange={(e) => setUserId(e.target.value)}
                required
              />
            </div>

            <div>
              <div className="flex justify-between items-center mb-2">
                <Label>Items</Label>
                <Button type="button" variant="secondary" size="sm" onClick={addItem}>
                  + Add Item
                </Button>
              </div>
              <div className="space-y-2">
                {items.map((item, index) => (
                  <div key={index} className="flex gap-2 items-center">
                    <select
                      value={item.productId}
                      onChange={(e) => updateItem(index, 'productId', Number(e.target.value))}
                      className="flex-1 h-11 rounded-xl border border-border bg-surface px-4 py-3 text-sm text-text transition-all duration-200 outline-none focus:border-primary focus:ring-4 focus:ring-primary-ring"
                    >
                      {products.map((p) => (
                        <option key={p.id} value={p.id}>
                          {p.name} (${p.price.toFixed(2)})
                        </option>
                      ))}
                    </select>
                    <Input
                      type="number"
                      value={item.quantity}
                      onChange={(e) =>
                        updateItem(index, 'quantity', Math.max(1, Number(e.target.value)))
                      }
                      min="1"
                      className="w-20"
                    />
                    {items.length > 1 && (
                      <Button
                        type="button"
                        variant="ghost"
                        size="sm"
                        onClick={() => removeItem(index)}
                      >
                        &times;
                      </Button>
                    )}
                  </div>
                ))}
              </div>
            </div>

            <div className="pt-2 border-t border-border">
              <div className="flex justify-between items-center text-lg font-semibold">
                <span>Estimated Total</span>
                <span>${getTotal().toFixed(2)}</span>
              </div>
            </div>

            <Button type="submit">Save Changes</Button>
          </form>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Danger Zone</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-text-muted mb-3">
            Permanently delete this order. This action cannot be undone.
          </p>
          <Button variant="danger" onClick={handleDelete}>
            Delete Order
          </Button>
        </CardContent>
      </Card>
    </div>
  );
}
```

**Step 2: Verify**

Run: `npm run check`

**Step 3: Commit**

```bash
git add src/modules/Orders/src/Orders/Pages/Edit.tsx
git commit -m "feat(orders): migrate Edit page to @simplemodule/ui components"
```

---

### Task 4: Migrate Users/Users.tsx

**Files:**
- Modify: `src/modules/Users/src/Users/Pages/Admin/Users.tsx`

**Step 1: Rewrite Users.tsx using UI components**

```tsx
import { router } from '@inertiajs/react';
import { type FormEvent, useState } from 'react';
import {
  Badge,
  Button,
  Input,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';

interface User {
  id: string;
  displayName: string;
  email: string;
  emailConfirmed: boolean;
  roles: string[];
  isLockedOut: boolean;
  createdAt: string;
}

interface Props {
  users: User[];
  search: string;
  page: number;
  totalPages: number;
  totalCount: number;
}

export default function Users({ users, search, page, totalPages, totalCount }: Props) {
  const [searchValue, setSearchValue] = useState(search);

  function handleSearch(e: FormEvent) {
    e.preventDefault();
    router.get('/admin/users', { search: searchValue, page: 1 }, { preserveState: true });
  }

  function goToPage(p: number) {
    router.get('/admin/users', { search: searchValue, page: p }, { preserveState: true });
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1 className="text-2xl font-extrabold tracking-tight">Users</h1>
          <p className="text-text-muted text-sm mt-1">{totalCount} total users</p>
        </div>
      </div>

      <form onSubmit={handleSearch} className="mb-6 flex gap-2">
        <Input
          value={searchValue}
          onChange={(e) => setSearchValue(e.target.value)}
          placeholder="Search by name or email..."
          className="flex-1"
        />
        <Button type="submit">Search</Button>
      </form>

      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Name</TableHead>
            <TableHead>Email</TableHead>
            <TableHead>Roles</TableHead>
            <TableHead>Status</TableHead>
            <TableHead>Created</TableHead>
            <TableHead />
          </TableRow>
        </TableHeader>
        <TableBody>
          {users.map((user) => (
            <TableRow key={user.id}>
              <TableCell className="font-medium">{user.displayName || '\u2014'}</TableCell>
              <TableCell className="text-text-secondary">
                {user.email}
                {!user.emailConfirmed && (
                  <Badge variant="warning" className="ml-2">
                    unverified
                  </Badge>
                )}
              </TableCell>
              <TableCell>
                <div className="flex gap-1 flex-wrap">
                  {user.roles.map((role) => (
                    <Badge key={role} variant="info">
                      {role}
                    </Badge>
                  ))}
                </div>
              </TableCell>
              <TableCell>
                {user.isLockedOut ? (
                  <Badge variant="danger">Locked</Badge>
                ) : (
                  <Badge variant="success">Active</Badge>
                )}
              </TableCell>
              <TableCell className="text-sm text-text-muted">
                {new Date(user.createdAt).toLocaleDateString()}
              </TableCell>
              <TableCell>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => router.get(`/admin/users/${user.id}/edit`)}
                >
                  Edit
                </Button>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>

      {totalPages > 1 && (
        <div className="flex justify-center gap-2 mt-6">
          <Button
            variant="secondary"
            size="sm"
            onClick={() => goToPage(page - 1)}
            disabled={page <= 1}
          >
            Previous
          </Button>
          <span className="px-3 py-1 text-text-muted text-sm">
            Page {page} of {totalPages}
          </span>
          <Button
            variant="secondary"
            size="sm"
            onClick={() => goToPage(page + 1)}
            disabled={page >= totalPages}
          >
            Next
          </Button>
        </div>
      )}
    </div>
  );
}
```

**Step 2: Verify**

Run: `npm run check`

**Step 3: Commit**

```bash
git add src/modules/Users/src/Users/Pages/Admin/Users.tsx
git commit -m "feat(users): migrate Users list page to @simplemodule/ui components"
```

---

### Task 5: Migrate Users/UsersEdit.tsx

**Files:**
- Modify: `src/modules/Users/src/Users/Pages/Admin/UsersEdit.tsx`

**Step 1: Rewrite UsersEdit.tsx using UI components**

Note: Keep native `<input type="checkbox">` for form submission with `name`/`value`/`defaultChecked` since the Radix Checkbox has a different API for uncontrolled forms. Style them to match.

```tsx
import { router } from '@inertiajs/react';
import {
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Input,
  Label,
} from '@simplemodule/ui';

interface UserDetail {
  id: string;
  displayName: string;
  email: string;
  emailConfirmed: boolean;
  isLockedOut: boolean;
  createdAt: string;
  lastLoginAt: string | null;
}

interface Role {
  id: string;
  name: string;
  description: string | null;
}

interface Props {
  user: UserDetail;
  userRoles: string[];
  allRoles: Role[];
}

export default function UsersEdit({ user, userRoles, allRoles }: Props) {
  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post(`/admin/users/${user.id}`, formData);
  }

  function handleRolesSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post(`/admin/users/${user.id}/roles`, formData);
  }

  function handleLock() {
    router.post(`/admin/users/${user.id}/lock`);
  }

  function handleUnlock() {
    router.post(`/admin/users/${user.id}/unlock`);
  }

  return (
    <div className="max-w-3xl">
      <div className="flex items-center gap-3 mb-1">
        <a
          href="/admin/users"
          className="text-text-muted hover:text-text transition-colors no-underline"
        >
          <svg
            className="w-4 h-4"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            viewBox="0 0 24 24"
          >
            <path d="M15 19l-7-7 7-7" />
          </svg>
        </a>
        <h1 className="text-2xl font-extrabold tracking-tight">Edit User</h1>
      </div>
      <p className="text-text-muted text-sm ml-7 mb-6">
        Created: {new Date(user.createdAt).toLocaleString()}
        {user.lastLoginAt && (
          <span className="ml-4">Last login: {new Date(user.lastLoginAt).toLocaleString()}</span>
        )}
      </p>

      <Card className="mb-6">
        <CardHeader>
          <CardTitle>Details</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <Label htmlFor="displayName">Display Name</Label>
              <Input id="displayName" name="displayName" defaultValue={user.displayName} />
            </div>
            <div>
              <Label htmlFor="email">Email</Label>
              <Input id="email" name="email" type="email" defaultValue={user.email} />
            </div>
            <div className="flex items-center gap-2">
              <input
                type="checkbox"
                name="emailConfirmed"
                id="emailConfirmed"
                defaultChecked={user.emailConfirmed}
                className="h-5 w-5 rounded-md border border-border bg-surface accent-primary"
              />
              <Label htmlFor="emailConfirmed" className="mb-0">
                Email confirmed
              </Label>
            </div>
            <Button type="submit">Save Details</Button>
          </form>
        </CardContent>
      </Card>

      <Card className="mb-6">
        <CardHeader>
          <CardTitle>Roles</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleRolesSubmit}>
            <div className="space-y-2 mb-4">
              {allRoles.map((role) => (
                <div key={role.id} className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    name="roles"
                    value={role.name ?? ''}
                    id={`role-${role.id}`}
                    defaultChecked={userRoles.includes(role.name ?? '')}
                    className="h-5 w-5 rounded-md border border-border bg-surface accent-primary"
                  />
                  <Label htmlFor={`role-${role.id}`} className="mb-0">
                    {role.name}
                    {role.description && (
                      <span className="text-text-muted ml-1">&mdash; {role.description}</span>
                    )}
                  </Label>
                </div>
              ))}
              {allRoles.length === 0 && (
                <p className="text-sm text-text-muted">No roles defined.</p>
              )}
            </div>
            <Button type="submit">Save Roles</Button>
          </form>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Account Status</CardTitle>
        </CardHeader>
        <CardContent>
          {user.isLockedOut ? (
            <div>
              <p className="text-sm text-danger mb-3">This account is locked.</p>
              <Button variant="outline" onClick={handleUnlock}>
                Unlock Account
              </Button>
            </div>
          ) : (
            <div>
              <p className="text-sm text-success mb-3">This account is active.</p>
              <Button variant="danger" onClick={handleLock}>
                Lock Account
              </Button>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
```

**Step 2: Verify**

Run: `npm run check`

**Step 3: Commit**

```bash
git add src/modules/Users/src/Users/Pages/Admin/UsersEdit.tsx
git commit -m "feat(users): migrate UsersEdit page to @simplemodule/ui components"
```

---

### Task 6: Migrate Users/Roles.tsx

**Files:**
- Modify: `src/modules/Users/src/Users/Pages/Admin/Roles.tsx`

**Step 1: Rewrite Roles.tsx using UI components**

```tsx
import { router } from '@inertiajs/react';
import {
  Badge,
  Button,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';

interface Role {
  id: string;
  name: string;
  description: string | null;
  userCount: number;
  createdAt: string;
}

interface Props {
  roles: Role[];
}

export default function Roles({ roles }: Props) {
  function handleDelete(id: string, name: string) {
    if (!confirm(`Delete role "${name}"?`)) return;
    router.delete(`/admin/roles/${id}`, {
      onError: () => alert('Cannot delete role with assigned users.'),
    });
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1 className="text-2xl font-extrabold tracking-tight">Roles</h1>
          <p className="text-text-muted text-sm mt-1">Manage application roles</p>
        </div>
        <Button onClick={() => router.get('/admin/roles/create')}>Create Role</Button>
      </div>

      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Name</TableHead>
            <TableHead>Description</TableHead>
            <TableHead>Users</TableHead>
            <TableHead>Created</TableHead>
            <TableHead />
          </TableRow>
        </TableHeader>
        <TableBody>
          {roles.map((role) => (
            <TableRow key={role.id}>
              <TableCell className="font-medium">{role.name}</TableCell>
              <TableCell className="text-text-secondary">{role.description || '\u2014'}</TableCell>
              <TableCell>
                <Badge variant="info">{role.userCount}</Badge>
              </TableCell>
              <TableCell className="text-sm text-text-muted">
                {new Date(role.createdAt).toLocaleDateString()}
              </TableCell>
              <TableCell>
                <div className="flex gap-3">
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => router.get(`/admin/roles/${role.id}/edit`)}
                  >
                    Edit
                  </Button>
                  <Button
                    variant="danger"
                    size="sm"
                    onClick={() => handleDelete(role.id, role.name)}
                  >
                    Delete
                  </Button>
                </div>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}
```

**Step 2: Verify**

Run: `npm run check`

**Step 3: Commit**

```bash
git add src/modules/Users/src/Users/Pages/Admin/Roles.tsx
git commit -m "feat(users): migrate Roles list page to @simplemodule/ui components"
```

---

### Task 7: Migrate Users/RolesCreate.tsx

**Files:**
- Modify: `src/modules/Users/src/Users/Pages/Admin/RolesCreate.tsx`

**Step 1: Rewrite RolesCreate.tsx using UI components**

```tsx
import { router } from '@inertiajs/react';
import { Button, Card, CardContent, Input, Label } from '@simplemodule/ui';

export default function RolesCreate() {
  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post('/admin/roles', formData);
  }

  return (
    <div className="max-w-xl">
      <div className="flex items-center gap-3 mb-1">
        <a
          href="/admin/roles"
          className="text-text-muted hover:text-text transition-colors no-underline"
        >
          <svg
            className="w-4 h-4"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            viewBox="0 0 24 24"
          >
            <path d="M15 19l-7-7 7-7" />
          </svg>
        </a>
        <h1 className="text-2xl font-extrabold tracking-tight">Create Role</h1>
      </div>
      <p className="text-text-muted text-sm ml-7 mb-6">Add a new application role</p>

      <Card>
        <CardContent className="p-6">
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <Label htmlFor="name">Name</Label>
              <Input id="name" name="name" required />
            </div>
            <div>
              <Label htmlFor="description">Description</Label>
              <Input id="description" name="description" />
            </div>
            <Button type="submit">Create</Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
```

**Step 2: Verify**

Run: `npm run check`

**Step 3: Commit**

```bash
git add src/modules/Users/src/Users/Pages/Admin/RolesCreate.tsx
git commit -m "feat(users): migrate RolesCreate page to @simplemodule/ui components"
```

---

### Task 8: Migrate Users/RolesEdit.tsx

**Files:**
- Modify: `src/modules/Users/src/Users/Pages/Admin/RolesEdit.tsx`

**Step 1: Rewrite RolesEdit.tsx using UI components**

```tsx
import { router } from '@inertiajs/react';
import {
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Input,
  Label,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';

interface RoleDetail {
  id: string;
  name: string;
  description: string | null;
  createdAt: string;
}

interface UserSummary {
  id: string;
  displayName: string;
  email: string;
}

interface Props {
  role: RoleDetail;
  users: UserSummary[];
}

export default function RolesEdit({ role, users }: Props) {
  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post(`/admin/roles/${role.id}`, formData);
  }

  return (
    <div className="max-w-xl">
      <div className="flex items-center gap-3 mb-1">
        <a
          href="/admin/roles"
          className="text-text-muted hover:text-text transition-colors no-underline"
        >
          <svg
            className="w-4 h-4"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            viewBox="0 0 24 24"
          >
            <path d="M15 19l-7-7 7-7" />
          </svg>
        </a>
        <h1 className="text-2xl font-extrabold tracking-tight">Edit Role</h1>
      </div>
      <p className="text-text-muted text-sm ml-7 mb-6">
        Created: {new Date(role.createdAt).toLocaleString()}
      </p>

      <Card className="mb-6">
        <CardContent className="p-6">
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <Label htmlFor="name">Name</Label>
              <Input id="name" name="name" defaultValue={role.name} required />
            </div>
            <div>
              <Label htmlFor="description">Description</Label>
              <Input id="description" name="description" defaultValue={role.description ?? ''} />
            </div>
            <Button type="submit">Save</Button>
          </form>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Assigned Users ({users.length})</CardTitle>
        </CardHeader>
        <CardContent>
          {users.length === 0 ? (
            <p className="text-sm text-text-muted">No users assigned to this role.</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Email</TableHead>
                  <TableHead />
                </TableRow>
              </TableHeader>
              <TableBody>
                {users.map((user) => (
                  <TableRow key={user.id}>
                    <TableCell className="font-medium">
                      {user.displayName || '\u2014'}
                    </TableCell>
                    <TableCell className="text-text-muted">{user.email}</TableCell>
                    <TableCell>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => router.get(`/admin/users/${user.id}/edit`)}
                      >
                        Edit
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
```

**Step 2: Verify**

Run: `npm run check`

**Step 3: Commit**

```bash
git add src/modules/Users/src/Users/Pages/Admin/RolesEdit.tsx
git commit -m "feat(users): migrate RolesEdit page to @simplemodule/ui components"
```

---

### Task 9: Migrate Products/Browse.tsx

**Files:**
- Modify: `src/modules/Products/src/Products/Views/Browse.tsx`

**Step 1: Rewrite Browse.tsx using UI components**

```tsx
import { Card, CardContent } from '@simplemodule/ui';

interface Product {
  id: number;
  name: string;
  price: number;
}

export default function Browse({ products }: { products: Product[] }) {
  return (
    <div className="max-w-4xl mx-auto">
      <h1 className="text-2xl font-extrabold tracking-tight mb-6">Products</h1>
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
```

**Step 2: Verify**

Run: `npm run check`

**Step 3: Commit**

```bash
git add src/modules/Products/src/Products/Views/Browse.tsx
git commit -m "feat(products): migrate Browse page to @simplemodule/ui components"
```

---

### Task 10: Migrate Products/Edit.tsx

**Files:**
- Modify: `src/modules/Products/src/Products/Views/Edit.tsx`

**Step 1: Rewrite Edit.tsx using UI components (match Create.tsx pattern)**

```tsx
import { router } from '@inertiajs/react';
import {
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Input,
  Label,
} from '@simplemodule/ui';

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
```

**Step 2: Verify**

Run: `npm run check`

**Step 3: Commit**

```bash
git add src/modules/Products/src/Products/Views/Edit.tsx
git commit -m "feat(products): migrate Edit page to @simplemodule/ui components"
```

---

### Task 11: Migrate Products/Manage.tsx (clean up remaining custom classes)

**Files:**
- Modify: `src/modules/Products/src/Products/Views/Manage.tsx`

**Step 1: Remove glass-card and gradient-text from already-migrated Manage.tsx**

Remove `className="glass-card overflow-x-auto"` wrapper div and `gradient-text` span. The Table component and heading provide their own styling.

```tsx
import { router } from '@inertiajs/react';
import { Button, Table, TableHeader, TableBody, TableRow, TableHead, TableCell } from '@simplemodule/ui';

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
          <h1 className="text-2xl font-extrabold tracking-tight">Manage Products</h1>
          <p className="text-text-muted text-sm mt-1">{products.length} total products</p>
        </div>
        <Button onClick={() => router.get('/products/create')}>Create Product</Button>
      </div>

      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>ID</TableHead>
            <TableHead>Name</TableHead>
            <TableHead>Price</TableHead>
            <TableHead />
          </TableRow>
        </TableHeader>
        <TableBody>
          {products.map((product) => (
            <TableRow key={product.id}>
              <TableCell className="text-text-muted">#{product.id}</TableCell>
              <TableCell className="font-medium text-text">{product.name}</TableCell>
              <TableCell>${product.price.toFixed(2)}</TableCell>
              <TableCell>
                <div className="flex gap-3">
                  <Button variant="ghost" size="sm" onClick={() => router.get(`/products/${product.id}/edit`)}>
                    Edit
                  </Button>
                  <Button variant="danger" size="sm" onClick={() => handleDelete(product.id, product.name)}>
                    Delete
                  </Button>
                </div>
              </TableCell>
            </TableRow>
          ))}
          {products.length === 0 && (
            <TableRow>
              <TableCell colSpan={4} className="py-8 text-center text-text-muted">
                No products yet. Create your first product!
              </TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
    </div>
  );
}
```

**Step 2: Verify**

Run: `npm run check`

**Step 3: Commit**

```bash
git add src/modules/Products/src/Products/Views/Manage.tsx
git commit -m "feat(products): clean up Manage page - remove glass-card and gradient-text"
```

---

### Task 12: Migrate Products/Create.tsx (clean up remaining custom classes)

**Files:**
- Modify: `src/modules/Products/src/Products/Views/Create.tsx`

**Step 1: Remove glass-card and gradient-text from already-migrated Create.tsx**

```tsx
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
        <h1 className="text-2xl font-extrabold tracking-tight">Create Product</h1>
      </div>
      <p className="text-text-muted text-sm ml-7 mb-6">Add a new product</p>

      <Card>
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
```

**Step 2: Verify**

Run: `npm run check`

**Step 3: Commit**

```bash
git add src/modules/Products/src/Products/Views/Create.tsx
git commit -m "feat(products): clean up Create page - remove glass-card and gradient-text"
```

---

### Task 13: Migrate Dashboard/Home.tsx

**Files:**
- Modify: `src/modules/Dashboard/src/Dashboard/Pages/Home.tsx`

**Step 1: Rewrite Home.tsx using UI components**

This is the largest file. Replace panel/glass-card/btn-*/badge-*/spinner patterns with Card, Button, Badge, Spinner, Table, Alert components.

```tsx
import React from 'react';
import {
  Alert,
  AlertDescription,
  AlertTitle,
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Spinner,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';

interface HomeProps {
  isAuthenticated: boolean;
  displayName: string;
  isDevelopment: boolean;
}

export default function Home({ isAuthenticated, displayName, isDevelopment }: HomeProps) {
  return isAuthenticated ? (
    <DashboardView displayName={displayName} />
  ) : (
    <LandingView isDevelopment={isDevelopment} />
  );
}

// --- Dashboard View ---

function DashboardView({ displayName }: { displayName: string }) {
  return (
    <>
      <div className="mb-6">
        <h1 className="text-2xl font-extrabold tracking-tight">
          Welcome back, {displayName}
        </h1>
        <p className="text-text-muted text-sm mt-1">Here&apos;s your development dashboard</p>
      </div>

      {/* Quick Actions */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-8">
        <a href="/Identity/Account/Manage" className="no-underline">
          <Card className="h-full group">
            <CardContent>
              <div className="flex items-center gap-3 mb-3">
                <span className="w-9 h-9 rounded-xl flex items-center justify-center text-primary bg-primary-subtle">
                  <svg
                    className="w-[18px] h-[18px]"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2"
                    viewBox="0 0 24 24"
                    aria-hidden="true"
                  >
                    <path d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                  </svg>
                </span>
                <span className="text-sm font-semibold text-text group-hover:text-primary transition-colors">
                  Account
                </span>
              </div>
              <p className="text-xs text-text-muted">Manage your profile and security settings</p>
            </CardContent>
          </Card>
        </a>
        <a href="/swagger" className="no-underline">
          <Card className="h-full group">
            <CardContent>
              <div className="flex items-center gap-3 mb-3">
                <span className="w-9 h-9 rounded-xl flex items-center justify-center text-accent bg-success-bg">
                  <svg
                    className="w-[18px] h-[18px]"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2"
                    viewBox="0 0 24 24"
                    aria-hidden="true"
                  >
                    <path d="M10 20l4-16m4 4l4 4-4 4M6 16l-4-4 4-4" />
                  </svg>
                </span>
                <span className="text-sm font-semibold text-text group-hover:text-primary transition-colors">
                  API Docs
                </span>
              </div>
              <p className="text-xs text-text-muted">Explore endpoints and test requests</p>
            </CardContent>
          </Card>
        </a>
        <a href="/health/live" className="no-underline">
          <Card className="h-full group">
            <CardContent>
              <div className="flex items-center gap-3 mb-3">
                <span className="w-9 h-9 rounded-xl flex items-center justify-center text-info bg-info-bg">
                  <svg
                    className="w-[18px] h-[18px]"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2"
                    viewBox="0 0 24 24"
                    aria-hidden="true"
                  >
                    <path d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
                  </svg>
                </span>
                <span className="text-sm font-semibold text-text group-hover:text-primary transition-colors">
                  Health
                </span>
              </div>
              <p className="text-xs text-text-muted">Check system status and diagnostics</p>
            </CardContent>
          </Card>
        </a>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <UserInfoPanel />
        <TokenTester />
      </div>

      <ApiTester />
    </>
  );
}

// --- User Info Panel ---

interface UserInfo {
  displayName?: string;
  name?: string;
  email?: string;
  id?: string;
  roles?: string | string[];
}

function InfoRow({
  label,
  value,
  monospace,
}: {
  label: string;
  value: string;
  monospace?: boolean;
}) {
  return (
    <div className="flex justify-between items-center py-3 text-sm border-b border-border last:border-b-0">
      <span className="text-text-muted text-xs uppercase tracking-wide">{label}</span>
      <span
        className={monospace ? 'font-mono text-xs text-text-secondary' : 'font-medium text-text'}
      >
        {value}
      </span>
    </div>
  );
}

function UserInfoPanel() {
  const [userInfo, setUserInfo] = React.useState<UserInfo | null>(null);
  const [error, setError] = React.useState<string | null>(null);
  const [loading, setLoading] = React.useState(true);

  React.useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const res = await fetch('/api/users/me');
        if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);
        const data = await res.json();
        if (!cancelled) {
          setUserInfo(data);
          setLoading(false);
        }
      } catch (e: unknown) {
        if (!cancelled) {
          setError(e instanceof Error ? e.message : String(e));
          setLoading(false);
        }
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <Card>
      <CardHeader>
        <CardTitle>User Info</CardTitle>
      </CardHeader>
      <CardContent>
        {loading && (
          <div className="text-text-muted text-sm flex items-center gap-2">
            Loading user info
            <Spinner size="sm" />
          </div>
        )}
        {error && <span className="text-danger text-sm">Failed to load: {error}</span>}
        {userInfo && (
          <>
            <InfoRow label="Name" value={userInfo.displayName || userInfo.name || '-'} />
            <InfoRow label="Email" value={userInfo.email || '-'} />
            <InfoRow label="ID" value={userInfo.id || '-'} monospace />
            {userInfo.roles && (
              <InfoRow
                label="Roles"
                value={Array.isArray(userInfo.roles) ? userInfo.roles.join(', ') : userInfo.roles}
              />
            )}
          </>
        )}
      </CardContent>
    </Card>
  );
}

// --- Token Tester ---

function generateCodeVerifier(): string {
  const arr = new Uint8Array(32);
  crypto.getRandomValues(arr);
  return btoa(String.fromCharCode(...arr))
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=/g, '');
}

async function generateCodeChallenge(verifier: string): Promise<string> {
  const data = new TextEncoder().encode(verifier);
  const hash = await crypto.subtle.digest('SHA-256', data);
  return btoa(String.fromCharCode(...new Uint8Array(hash)))
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=/g, '');
}

interface DecodedClaim {
  key: string;
  value: string;
}

function decodeToken(token: string): DecodedClaim[] | null {
  try {
    const parts = token.split('.');
    const payload = JSON.parse(atob(parts[1].replace(/-/g, '+').replace(/_/g, '/')));
    return Object.keys(payload).map((key) => {
      const val = payload[key];
      const display =
        key === 'exp' || key === 'iat' || key === 'nbf'
          ? new Date(val * 1000).toLocaleString()
          : typeof val === 'object'
            ? JSON.stringify(val)
            : String(val);
      return { key, value: display };
    });
  } catch {
    return null;
  }
}

function TokenTester() {
  const [token, setToken] = React.useState<string | null>(null);
  const [authorizing, setAuthorizing] = React.useState(false);

  const claims = React.useMemo(() => (token ? decodeToken(token) : null), [token]);

  React.useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const code = params.get('code');
    const state = params.get('state');
    if (!code) return;

    window.history.replaceState({}, '', '/');

    const verifier = sessionStorage.getItem('pkce_verifier');
    const savedState = sessionStorage.getItem('pkce_state');

    if (state !== savedState) {
      alert('OAuth state mismatch');
      return;
    }

    sessionStorage.removeItem('pkce_verifier');
    sessionStorage.removeItem('pkce_state');

    (async () => {
      try {
        const res = await fetch('/connect/token', {
          method: 'POST',
          headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
          body: new URLSearchParams({
            grant_type: 'authorization_code',
            client_id: 'simplemodule-client',
            code,
            redirect_uri: `${window.location.origin}/oauth-callback`,
            code_verifier: verifier ?? '',
          }),
        });

        if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);
        const data = await res.json();
        setToken(data.access_token);
      } catch (e: unknown) {
        alert(`Token exchange failed: ${e instanceof Error ? e.message : String(e)}`);
      }
    })();
  }, []);

  const startOAuthFlow = async () => {
    setAuthorizing(true);

    const verifier = generateCodeVerifier();
    const challenge = await generateCodeChallenge(verifier);
    const state = crypto.randomUUID();

    sessionStorage.setItem('pkce_verifier', verifier);
    sessionStorage.setItem('pkce_state', state);

    const params = new URLSearchParams({
      response_type: 'code',
      client_id: 'simplemodule-client',
      redirect_uri: `${window.location.origin}/oauth-callback`,
      scope: 'openid profile email',
      state,
      code_challenge: challenge,
      code_challenge_method: 'S256',
    });

    window.location.href = `/connect/authorize?${params.toString()}`;
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Token Tester</CardTitle>
      </CardHeader>
      <CardContent>
        <h3 className="text-sm font-semibold mb-1">OAuth2 Authorization Code + PKCE</h3>
        <p className="text-xs text-text-muted mb-4">
          Obtain an access token using the{' '}
          <code className="bg-surface-raised px-1.5 py-0.5 rounded text-xs font-mono">
            simplemodule-client
          </code>{' '}
          application.
        </p>
        <Button size="sm" disabled={authorizing} onClick={startOAuthFlow}>
          {authorizing ? (
            <>
              Authorizing
              <Spinner size="sm" />
            </>
          ) : (
            'Get Access Token'
          )}
        </Button>
        {token && (
          <div>
            <div className="bg-surface-raised rounded-xl p-3 font-mono text-xs break-all max-h-30 overflow-auto mt-4">
              {token}
            </div>
            {claims && (
              <>
                <h3 className="text-sm font-semibold mt-4 mb-2">Decoded Claims</h3>
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Claim</TableHead>
                      <TableHead>Value</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {claims.map((claim) => (
                      <TableRow key={claim.key}>
                        <TableCell>{claim.key}</TableCell>
                        <TableCell>{claim.value}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </>
            )}
          </div>
        )}
      </CardContent>
    </Card>
  );
}

// --- API Tester ---

const API_ENDPOINTS = ['/api/users/me', '/api/users', '/api/products', '/api/orders'];

function ApiTester() {
  const [status, setStatus] = React.useState<{
    loading: boolean;
    ok?: boolean;
    code?: string;
    statusText?: string;
    url?: string;
    error?: string;
  } | null>(null);
  const [response, setResponse] = React.useState('Click an endpoint above to make a request.');

  const getAccessToken = (): string | null => {
    const codeBlocks = document.querySelectorAll('.font-mono.text-xs.break-all');
    for (const block of codeBlocks) {
      const text = block.textContent || '';
      if (text.includes('.') && text.length > 50) {
        return text.trim();
      }
    }
    return null;
  };

  const callApi = async (url: string) => {
    setStatus({ loading: true, url });
    setResponse('');

    const headers: Record<string, string> = {};
    const accessToken = getAccessToken();
    if (accessToken) {
      headers.Authorization = `Bearer ${accessToken}`;
    }

    try {
      const res = await fetch(url, { headers });
      const text = await res.text();
      setStatus({
        loading: false,
        ok: res.ok,
        code: String(res.status),
        statusText: res.statusText,
        url,
      });
      try {
        setResponse(JSON.stringify(JSON.parse(text), null, 2));
      } catch {
        setResponse(text);
      }
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : String(e);
      setStatus({ loading: false, ok: false, code: 'Error', error: msg, url });
      setResponse(msg);
    }
  };

  return (
    <Card className="mt-6">
      <CardHeader>
        <CardTitle>API Tester</CardTitle>
      </CardHeader>
      <CardContent>
        <h3 className="text-sm font-semibold mb-3">Call Protected Endpoints</h3>
        <div className="flex gap-2 flex-wrap mb-4">
          {API_ENDPOINTS.map((url) => (
            <Button key={url} variant="outline" size="sm" onClick={() => callApi(url)}>
              GET {url}
            </Button>
          ))}
        </div>
        <div className="text-xs text-text-muted mb-2 flex items-center gap-2">
          {status?.loading && (
            <>
              Calling {status.url}
              <Spinner size="sm" />
            </>
          )}
          {status && !status.loading && (
            <>
              <Badge variant={status.ok ? 'success' : 'danger'}>{status.code}</Badge>{' '}
              {status.error ? status.error : `${status.statusText} \u2014 ${status.url}`}
            </>
          )}
        </div>
        <div className="bg-surface-raised rounded-xl p-3 font-mono text-xs whitespace-pre-wrap max-h-50 overflow-auto">
          {response}
        </div>
      </CardContent>
    </Card>
  );
}

// --- Landing View ---

function LandingView({ isDevelopment }: { isDevelopment: boolean }) {
  return (
    <div className="flex items-center justify-center min-h-[calc(100vh-16rem)]">
      <div className="text-center max-w-lg mx-auto">
        <div
          className="w-16 h-16 rounded-2xl mx-auto mb-6 flex items-center justify-center text-white text-2xl font-bold shadow-lg"
          style={{
            background: 'linear-gradient(135deg, var(--color-primary), var(--color-accent))',
          }}
        >
          S
        </div>
        <h1 className="text-4xl font-extrabold mb-3 tracking-tight">SimpleModule</h1>
        <p className="text-text-muted text-base mb-8 max-w-sm mx-auto leading-relaxed">
          Modular monolith framework for .NET &mdash; AOT&#8209;compatible, zero&nbsp;reflection
        </p>

        <div className="flex gap-3 justify-center flex-wrap">
          <Button asChild size="lg">
            <a href="/Identity/Account/Login" className="no-underline">
              Get Started
            </a>
          </Button>
          <Button asChild variant="secondary" size="lg">
            <a href="/Identity/Account/Register" className="no-underline">
              Create Account
            </a>
          </Button>
        </div>

        {isDevelopment && (
          <Alert variant="warning" className="mt-6 text-left text-xs">
            <AlertTitle>Quick Start (Development Only)</AlertTitle>
            <AlertDescription>
              Email:{' '}
              <code className="bg-warning-bg px-1.5 py-0.5 rounded text-xs font-mono font-medium">
                admin@simplemodule.dev
              </code>
              &nbsp; Password:{' '}
              <code className="bg-warning-bg px-1.5 py-0.5 rounded text-xs font-mono font-medium">
                Admin123!
              </code>
            </AlertDescription>
          </Alert>
        )}

        <div className="flex gap-5 justify-center mt-8 text-sm">
          <a
            href="/swagger"
            className="text-text-muted no-underline hover:text-primary transition-colors"
          >
            API Docs
          </a>
          <span className="text-border">&middot;</span>
          <a
            href="/health/live"
            className="text-text-muted no-underline hover:text-primary transition-colors"
          >
            Health Check
          </a>
        </div>
      </div>
    </div>
  );
}
```

**Step 2: Verify**

Run: `npm run check`

**Step 3: Commit**

```bash
git add src/modules/Dashboard/src/Dashboard/Pages/Home.tsx
git commit -m "feat(dashboard): migrate Home page to @simplemodule/ui components"
```

---

### Task 14: Final verification

**Step 1: Run lint check across all workspaces**

Run: `npm run check`

**Step 2: Run .NET build to verify no compile errors**

Run: `dotnet build`

**Step 3: Commit any remaining fixes if needed**
