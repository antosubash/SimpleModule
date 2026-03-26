# Shared Page Components Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Create `PageShell` and `DataGridPage` shared components, then migrate all 14 pages for consistent layout.

**Architecture:** Two new composable components in `@simplemodule/ui`. `PageShell` wraps Container + breadcrumbs + PageHeader. `DataGridPage` extends `PageShell` with filter bar slot, built-in empty state, and DataGrid.

**Tech Stack:** React 19, TypeScript, @simplemodule/ui component library

---

### Task 1: Create PageShell Component

**Files:**
- Create: `packages/SimpleModule.UI/components/page-shell.tsx`
- Modify: `packages/SimpleModule.UI/components/index.ts`

**Step 1: Create `page-shell.tsx`**

```tsx
import * as React from 'react';
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from './breadcrumb';
import { Container, type ContainerProps } from './container';
import { PageHeader } from './page-header';

interface BreadcrumbEntry {
  label: string;
  href?: string;
}

interface PageShellProps {
  title: string;
  description?: string;
  actions?: React.ReactNode;
  breadcrumbs?: BreadcrumbEntry[];
  children: React.ReactNode;
  className?: string;
  size?: ContainerProps['size'];
}

function PageShell({
  title,
  description,
  actions,
  breadcrumbs,
  children,
  className,
  size,
}: PageShellProps) {
  return (
    <Container className={className ?? 'space-y-6'} size={size}>
      {breadcrumbs && breadcrumbs.length > 0 && (
        <Breadcrumb>
          <BreadcrumbList>
            {breadcrumbs.map((crumb, index) => (
              <React.Fragment key={crumb.label}>
                {index > 0 && <BreadcrumbSeparator />}
                <BreadcrumbItem>
                  {crumb.href ? (
                    <BreadcrumbLink href={crumb.href}>{crumb.label}</BreadcrumbLink>
                  ) : (
                    <BreadcrumbPage>{crumb.label}</BreadcrumbPage>
                  )}
                </BreadcrumbItem>
              </React.Fragment>
            ))}
          </BreadcrumbList>
        </Breadcrumb>
      )}
      <PageHeader className="mb-0" title={title} description={description} actions={actions} />
      {children}
    </Container>
  );
}

export type { BreadcrumbEntry, PageShellProps };
export { PageShell };
```

**Step 2: Export from index.ts**

Add to `packages/SimpleModule.UI/components/index.ts` (alphabetically near PageHeader):

```ts
export { PageShell, type BreadcrumbEntry, type PageShellProps } from './page-shell';
```

**Step 3: Build to verify**

Run: `npm run build --workspace=packages/SimpleModule.UI`
Expected: Clean build, no errors

**Step 4: Commit**

```
feat(ui): add PageShell component for consistent page layout
```

---

### Task 2: Create DataGridPage Component

**Files:**
- Create: `packages/SimpleModule.UI/components/data-grid-page.tsx`
- Modify: `packages/SimpleModule.UI/components/index.ts`

**Step 1: Create `data-grid-page.tsx`**

```tsx
import * as React from 'react';
import { Card, CardContent } from './card';
import { DataGrid } from './data-grid';
import { PageShell, type BreadcrumbEntry, type PageShellProps } from './page-shell';

interface DataGridPageProps<T> extends Omit<PageShellProps, 'children' | 'className'> {
  filterBar?: React.ReactNode;
  data: T[];
  pageSize?: number;
  pageSizeOptions?: number[];
  children: (pageData: T[]) => React.ReactNode;
  emptyTitle?: string;
  emptyDescription?: string;
  emptyIcon?: React.ReactNode;
  emptyAction?: React.ReactNode;
}

const defaultEmptyIcon = (
  <svg
    aria-hidden="true"
    className="mb-4 h-12 w-12 text-text-muted/50"
    fill="none"
    stroke="currentColor"
    strokeWidth="1.5"
    viewBox="0 0 24 24"
  >
    <path
      strokeLinecap="round"
      strokeLinejoin="round"
      d="M20.25 7.5l-.625 10.632a2.25 2.25 0 01-2.247 2.118H6.622a2.25 2.25 0 01-2.247-2.118L3.75 7.5M10 11.25h4M3.375 7.5h17.25c.621 0 1.125-.504 1.125-1.125v-1.5c0-.621-.504-1.125-1.125-1.125H3.375c-.621 0-1.125.504-1.125 1.125v1.5c0 .621.504 1.125 1.125 1.125z"
    />
  </svg>
);

function DataGridPage<T>({
  filterBar,
  data,
  pageSize,
  pageSizeOptions,
  children,
  emptyTitle = 'No items found',
  emptyDescription = 'Get started by creating your first item.',
  emptyIcon = defaultEmptyIcon,
  emptyAction,
  ...shellProps
}: DataGridPageProps<T>) {
  return (
    <PageShell {...shellProps}>
      {filterBar}
      {data.length === 0 ? (
        <Card>
          <CardContent>
            <div className="flex flex-col items-center justify-center py-12 text-center">
              {emptyIcon}
              <h3 className="text-sm font-medium">{emptyTitle}</h3>
              <p className="mt-1 text-sm text-text-muted">{emptyDescription}</p>
              {emptyAction && <div className="mt-4">{emptyAction}</div>}
            </div>
          </CardContent>
        </Card>
      ) : (
        <DataGrid data={data} pageSize={pageSize} pageSizeOptions={pageSizeOptions}>
          {children}
        </DataGrid>
      )}
    </PageShell>
  );
}

export type { DataGridPageProps };
export { DataGridPage };
```

**Step 2: Export from index.ts**

Add to `packages/SimpleModule.UI/components/index.ts`:

```ts
export { DataGridPage, type DataGridPageProps } from './data-grid-page';
```

**Step 3: Build to verify**

Run: `npm run build --workspace=packages/SimpleModule.UI`
Expected: Clean build, no errors

**Step 4: Commit**

```
feat(ui): add DataGridPage component for consistent list page layout
```

---

### Task 3: Migrate Products Module

**Files:**
- Modify: `modules/Products/src/Products/Views/Browse.tsx`
- Modify: `modules/Products/src/Products/Views/Manage.tsx`

**Step 1: Migrate Browse.tsx**

Replace the entire component body to use `PageShell`:

```tsx
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
```

**Step 2: Migrate Manage.tsx**

Replace the Container/PageHeader/empty-state/DataGrid assembly with `DataGridPage`. Keep the delete dialog outside:

```tsx
import { router } from '@inertiajs/react';
import {
  Button,
  DataGridPage,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';
import { useState } from 'react';
import type { Product } from '../types';

interface Props {
  products: Product[];
}

export default function Manage({ products }: Props) {
  const [deleteTarget, setDeleteTarget] = useState<{
    id: number;
    name: string;
  } | null>(null);

  function handleDelete() {
    if (!deleteTarget) return;
    router.delete(`/products/${deleteTarget.id}`);
    setDeleteTarget(null);
  }

  return (
    <>
      <DataGridPage
        title="Manage Products"
        description={`${products.length} total products`}
        actions={<Button onClick={() => router.get('/products/create')}>Create Product</Button>}
        data={products}
        emptyTitle="No products yet"
        emptyDescription="Get started by creating your first product."
      >
        {(pageData) => (
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
              {pageData.map((product) => (
                <TableRow key={product.id}>
                  <TableCell className="text-text-muted">#{product.id}</TableCell>
                  <TableCell className="font-medium text-text">{product.name}</TableCell>
                  <TableCell>${product.price.toFixed(2)}</TableCell>
                  <TableCell>
                    <div className="flex gap-3">
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => router.get(`/products/${product.id}/edit`)}
                      >
                        Edit
                      </Button>
                      <Button
                        variant="danger"
                        size="sm"
                        onClick={() => setDeleteTarget({ id: product.id, name: product.name })}
                      >
                        Delete
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </DataGridPage>

      <Dialog open={deleteTarget !== null} onOpenChange={(open) => !open && setDeleteTarget(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete Product</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete &ldquo;{deleteTarget?.name}&rdquo;? This action cannot
              be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="secondary" onClick={() => setDeleteTarget(null)}>
              Cancel
            </Button>
            <Button variant="danger" onClick={handleDelete}>
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
```

**Step 3: Build to verify**

Run: `npm run dev:build`
Expected: Clean build for Products module

**Step 4: Commit**

```
refactor(products): migrate to PageShell and DataGridPage components
```

---

### Task 4: Migrate Admin Module (Users + Roles)

**Files:**
- Modify: `modules/Admin/src/Admin/Pages/Admin/Users.tsx`
- Modify: `modules/Admin/src/Admin/Pages/Admin/Roles.tsx`

**Step 1: Migrate Users.tsx**

This page has server-side pagination, so it cannot use `DataGridPage` (which wraps client-side `DataGrid`). Use `PageShell` instead. Keep the custom pagination and search form.

Replace `<Container className="space-y-6">` + `<PageHeader className="mb-0" .../>` with `<PageShell>`:

```tsx
import { router } from '@inertiajs/react';
import {
  Badge,
  Button,
  Input,
  PageShell,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';
import { type FormEvent, useState } from 'react';

// ... interface definitions stay the same ...

export default function Users({ users, search, page, totalPages, totalCount }: Props) {
  const [searchValue, setSearchValue] = useState(search);

  // ... functions stay the same ...

  return (
    <PageShell
      title="Users"
      description={`${totalCount} total users`}
      actions={<Button onClick={() => router.get('/admin/users/create')}>Create User</Button>}
    >
      <form onSubmit={handleSearch} className="flex gap-2">
        {/* ... same form content ... */}
      </form>

      <Table>
        {/* ... same table content ... */}
      </Table>

      {/* ... same empty state and pagination ... */}
    </PageShell>
  );
}
```

Key change: Replace `<Container className="space-y-6">` + `<PageHeader className="mb-0" .../>` with `<PageShell ...>`. Everything else (search form, table, custom pagination) stays as children.

**Step 2: Migrate Roles.tsx**

Replace Container/PageHeader/empty-state/DataGrid with `DataGridPage`. Keep the error alert and dialog outside.

Replace the component to use `DataGridPage`:
- Remove `Container`, `PageHeader`, `Card`, `CardContent` from imports
- Add `DataGridPage` to imports
- Wrap in `<>` fragment: `<DataGridPage ...>` + the delete dialog
- Move the `deleteError` alert into the `filterBar` slot (it sits between header and grid)

```tsx
import { router } from '@inertiajs/react';
import {
  Badge,
  Button,
  DataGridPage,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';
import { useState } from 'react';

// ... interfaces stay the same ...

export default function Roles({ roles }: Props) {
  // ... state and handlers stay the same ...

  const errorAlert = deleteError ? (
    <div className="rounded-lg border border-danger/30 bg-danger/10 px-4 py-3 text-sm text-danger flex items-center justify-between">
      <span>{deleteError}</span>
      <button
        type="button"
        className="text-danger hover:text-danger/80"
        onClick={() => setDeleteError(null)}
      >
        <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24" aria-hidden="true">
          <path d="M18 6 6 18M6 6l12 12" />
        </svg>
      </button>
    </div>
  ) : null;

  return (
    <>
      <DataGridPage
        title="Roles"
        description="Manage application roles and permissions."
        actions={<Button onClick={() => router.get('/admin/roles/create')}>Create Role</Button>}
        data={roles}
        filterBar={errorAlert}
      >
        {(pageData) => (
          <Table>
            {/* ... same table content ... */}
          </Table>
        )}
      </DataGridPage>

      {/* ... same delete dialog ... */}
    </>
  );
}
```

**Step 3: Build to verify**

Run: `npm run dev:build`
Expected: Clean build for Admin module

**Step 4: Commit**

```
refactor(admin): migrate Users and Roles pages to PageShell/DataGridPage
```

---

### Task 5: Migrate Orders Module

**Files:**
- Modify: `modules/Orders/src/Orders/Pages/List.tsx`

**Step 1: Migrate List.tsx**

Same pattern as Products/Manage — replace Container/PageHeader/empty-state/DataGrid with `DataGridPage`:

```tsx
import { router } from '@inertiajs/react';
import {
  Badge,
  Button,
  DataGridPage,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';
import { useState } from 'react';
import type { Order } from '../types';

interface Props {
  orders: Order[];
}

export default function List({ orders }: Props) {
  const [deleteId, setDeleteId] = useState<number | null>(null);

  function handleDelete() {
    if (deleteId === null) return;
    router.delete(`/orders/${deleteId}`);
    setDeleteId(null);
  }

  return (
    <>
      <DataGridPage
        title="Orders"
        description={`${orders.length} total orders`}
        actions={<Button onClick={() => router.get('/orders/create')}>Create Order</Button>}
        data={orders}
        emptyTitle="No orders yet"
        emptyDescription="Get started by creating your first order."
      >
        {(pageData) => (
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
              {pageData.map((order) => (
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
                      <Button variant="danger" size="sm" onClick={() => setDeleteId(order.id)}>
                        Delete
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </DataGridPage>

      <Dialog open={deleteId !== null} onOpenChange={(open) => !open && setDeleteId(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete Order</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete order #{deleteId}? This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="secondary" onClick={() => setDeleteId(null)}>
              Cancel
            </Button>
            <Button variant="danger" onClick={handleDelete}>
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
```

**Step 2: Build to verify**

Run: `npm run dev:build`
Expected: Clean build for Orders module

**Step 3: Commit**

```
refactor(orders): migrate List page to DataGridPage
```

---

### Task 6: Migrate OpenIddict Module

**Files:**
- Modify: `modules/OpenIddict/src/OpenIddict/Pages/OpenIddict/Clients.tsx`

**Step 1: Migrate Clients.tsx**

Same pattern — replace Container/PageHeader/empty-state/DataGrid with `DataGridPage`:

```tsx
import { router } from '@inertiajs/react';
import {
  Badge,
  Button,
  DataGridPage,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';
import { useState } from 'react';

// ... interfaces stay the same ...

export default function Clients({ clients }: Props) {
  // ... state and handlers stay the same ...

  return (
    <>
      <DataGridPage
        title="Clients"
        description={`${clients.length} registered ${clients.length === 1 ? 'client' : 'clients'}`}
        actions={
          <Button onClick={() => router.get('/openiddict/clients/create')}>Create Client</Button>
        }
        data={clients}
        emptyTitle="No clients yet"
        emptyDescription="Get started by registering your first OpenID Connect client."
      >
        {(pageData) => (
          <Table>
            {/* ... same table content ... */}
          </Table>
        )}
      </DataGridPage>

      {/* ... same delete dialog ... */}
    </>
  );
}
```

**Step 2: Build to verify**

Run: `npm run dev:build`
Expected: Clean build for OpenIddict module

**Step 3: Commit**

```
refactor(openiddict): migrate Clients page to DataGridPage
```

---

### Task 7: Migrate AuditLogs Module (Browse, Dashboard, Detail)

**Files:**
- Modify: `modules/AuditLogs/src/AuditLogs/Views/Browse.tsx`
- Modify: `modules/AuditLogs/src/AuditLogs/Views/Dashboard.tsx`
- Modify: `modules/AuditLogs/src/AuditLogs/Views/Detail.tsx`

**Step 1: Migrate Browse.tsx**

Use `DataGridPage` with the filter Card passed as `filterBar`:

- Replace `Container`, `PageHeader` imports with `DataGridPage`
- Keep `Card`/`CardContent` imports (needed for the filter bar)
- Remove the empty state Card (handled by DataGridPage)
- Pass the filter Card as `filterBar` prop
- Pass `result.items` as `data`

Key changes:
```tsx
// Import DataGridPage instead of Container/PageHeader/DataGrid
import { ..., DataGridPage, ... } from '@simplemodule/ui';

// The filter panel JSX (extract to a variable):
const filterPanel = (
  <Card>
    <CardContent>
      {/* ... same 6-column grid of filters + Apply/Clear buttons ... */}
    </CardContent>
  </Card>
);

// Replace Container/PageHeader/empty-state/DataGrid with:
return (
  <DataGridPage
    title="Audit Logs"
    description={`${result.totalCount} total entries`}
    actions={/* same export buttons */}
    data={result.items}
    filterBar={filterPanel}
    emptyTitle="No audit logs found"
    emptyDescription="Try adjusting your filters or check back later."
  >
    {(pageData) => (
      <Table>
        {/* ... same table content ... */}
      </Table>
    )}
  </DataGridPage>
);
```

**Step 2: Migrate Dashboard.tsx**

Use `PageShell`. Replace `<Container className="space-y-4">` + `<PageHeader className="mb-0" .../>` with `<PageShell className="space-y-4" ...>`:

```tsx
// Replace Container/PageHeader imports with PageShell
import { ..., PageShell, ... } from '@simplemodule/ui';

// Replace:
//   <Container className="space-y-4">
//     <PageHeader className="mb-0" title="Audit Dashboard" ... />
// With:
//   <PageShell className="space-y-4" title="Audit Dashboard" ...>
```

Note: Dashboard uses `space-y-4` not `space-y-6`, so pass `className="space-y-4"` to override.

**Step 3: Migrate Detail.tsx**

Use `PageShell` with breadcrumbs:

```tsx
// Replace Container/PageHeader imports with PageShell
import { ..., PageShell, ... } from '@simplemodule/ui';

// Replace:
//   <Container className="space-y-6">
//     <PageHeader className="mb-0" title={...} actions={...} />
// With:
return (
  <PageShell
    title={`Audit Entry #${entry.id}`}
    actions={
      <Button variant="secondary" onClick={() => router.get('/audit-logs/browse')}>
        Back to Browse
      </Button>
    }
    breadcrumbs={[
      { label: 'Audit Logs', href: '/audit-logs/browse' },
      { label: `Entry #${entry.id}` },
    ]}
  >
    {/* ... same card content ... */}
  </PageShell>
);
```

**Step 4: Build to verify**

Run: `npm run dev:build`
Expected: Clean build for AuditLogs module

**Step 5: Commit**

```
refactor(auditlogs): migrate Browse, Dashboard, and Detail to PageShell/DataGridPage
```

---

### Task 8: Migrate PageBuilder Module (PagesList + Manage)

**Files:**
- Modify: `modules/PageBuilder/src/PageBuilder/Views/PagesList.tsx`
- Modify: `modules/PageBuilder/src/PageBuilder/Views/Manage.tsx`

**Step 1: Migrate PagesList.tsx**

Replace manual `<h1>` + Container with `PageShell`:

```tsx
import { Link } from '@inertiajs/react';
import { Card, CardContent, PageShell } from '@simplemodule/ui';
import type { PageSummary } from '../types';

interface Props {
  pages: PageSummary[];
}

export default function PagesList({ pages }: Props) {
  return (
    <PageShell title="Pages">
      {pages.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-12 text-center">
          {/* ... same SVG and text ... */}
        </div>
      ) : (
        <div className="space-y-3">
          {pages.map((page) => (
            <Link key={page.id} href={`/p/${page.slug}`}>
              <Card className="hover:bg-surface-elevated transition-colors cursor-pointer">
                <CardContent className="p-4">
                  <span className="font-medium">{page.title}</span>
                </CardContent>
              </Card>
            </Link>
          ))}
        </div>
      )}
    </PageShell>
  );
}
```

**Step 2: Migrate Manage.tsx**

Replace manual `<h1>` + `<div flex>` header + Container with `DataGridPage`:

```tsx
import { router } from '@inertiajs/react';
import {
  Badge,
  Button,
  DataGridPage,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  Input,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';
import { useState } from 'react';
import type { PageSummary } from '../types';

// ... same interfaces and handlers ...

export default function Manage({ pages }: Props) {
  // ... same state and handlers ...

  return (
    <>
      <DataGridPage
        title="Pages"
        description="Manage content pages"
        actions={<Button onClick={() => router.get('/admin/pages/new')}>New Page</Button>}
        data={pages}
        emptyTitle="No pages yet"
        emptyDescription="Get started by creating your first content page."
        emptyAction={
          <Button size="sm" onClick={() => router.get('/admin/pages/new')}>
            New Page
          </Button>
        }
      >
        {(pageData) => (
          <Table>
            {/* ... same table content ... */}
          </Table>
        )}
      </DataGridPage>

      {/* ... same delete dialog ... */}
    </>
  );
}
```

**Step 3: Build to verify**

Run: `npm run dev:build`
Expected: Clean build for PageBuilder module

**Step 4: Commit**

```
refactor(pagebuilder): migrate PagesList and Manage to PageShell/DataGridPage
```

---

### Task 9: Migrate Settings Module (AdminSettings + MenuManager)

**Files:**
- Modify: `modules/Settings/src/Settings/Views/AdminSettings.tsx`
- Modify: `modules/Settings/src/Settings/Views/MenuManager.tsx`

**Step 1: Migrate AdminSettings.tsx**

Replace manual `<h1>` + Container with `PageShell`:

```tsx
import { PageShell, Tabs, TabsContent, TabsList, TabsTrigger } from '@simplemodule/ui';
// ... (remove Container import)

export default function AdminSettings({ definitions, settings }: AdminSettingsProps) {
  // ... same state and handlers ...

  return (
    <PageShell title="Settings">
      <Tabs defaultValue="system">
        {/* ... same tab content ... */}
      </Tabs>
    </PageShell>
  );
}
```

**Step 2: Migrate MenuManager.tsx**

Replace manual Breadcrumb + PageHeader + Container with `PageShell` using `breadcrumbs` prop:

```tsx
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  PageShell,
  ScrollArea,
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@simplemodule/ui';
// Remove: Breadcrumb, BreadcrumbItem, BreadcrumbLink, BreadcrumbList, BreadcrumbPage,
//         BreadcrumbSeparator, Container, PageHeader

export default function MenuManager({ menus: initial, availablePages }: MenuManagerProps) {
  // ... same state and handlers ...

  return (
    <TooltipProvider>
      <PageShell
        title="Menu Manager"
        description="Configure the public navigation menu. Add, reorder, and organize menu items."
        breadcrumbs={[
          { label: 'Settings', href: '/admin/settings' },
          { label: 'Menu Manager' },
        ]}
      >
        <div className="grid grid-cols-1 gap-6 md:grid-cols-[2fr_3fr]">
          {/* ... same grid content ... */}
        </div>
      </PageShell>
    </TooltipProvider>
  );
}
```

**Step 3: Build to verify**

Run: `npm run dev:build`
Expected: Clean build for Settings module

**Step 4: Commit**

```
refactor(settings): migrate AdminSettings and MenuManager to PageShell
```

---

### Task 10: Migrate Dashboard Module

**Files:**
- Modify: `modules/Dashboard/src/Dashboard/Pages/Home.tsx`

**Step 1: Migrate DashboardView function**

Only the `DashboardView` function uses PageHeader. The `LandingView` uses a custom centered layout and should stay as-is.

Replace the `DashboardView` function:

```tsx
function DashboardView({ displayName }: { displayName: string }) {
  return (
    <PageShell
      title={`Welcome back, ${displayName}`}
      description="Here's your development dashboard"
    >
      {/* Quick Actions */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-8">
        {/* ... same cards ... */}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <UserInfoPanel />
        <TokenTester />
      </div>

      <ApiTester />
    </PageShell>
  );
}
```

Update imports: replace `Container, PageHeader` with `PageShell`. Keep `Container` import because `LandingView` still uses it.

**Step 2: Build to verify**

Run: `npm run dev:build`
Expected: Clean build for Dashboard module

**Step 3: Commit**

```
refactor(dashboard): migrate Home page to PageShell
```

---

### Task 11: Final Verification

**Step 1: Full build**

Run: `npm run build`
Expected: Clean production build, no errors

**Step 2: Lint check**

Run: `npm run check`
Expected: No lint or formatting errors

**Step 3: .NET build**

Run: `dotnet build`
Expected: Clean build

**Step 4: Commit any remaining fixes**

If lint/build fixes were needed, commit them:

```
fix: resolve lint and build issues from shared component migration
```
