# Shared Page Components Design

**Date:** 2026-03-26
**Goal:** Create shared layout components for consistent page structure across all modules.

## Problem

Pages across modules have inconsistent header structures:
- Most use `<PageHeader>` but some (PageBuilder, Settings) use manual `<h1>` tags
- Every page repeats Container + PageHeader + `className="mb-0"` assembly
- Data grid pages independently handle empty states, filters, and pagination
- Admin Users page has custom pagination instead of using DataGrid

## Solution: Two Shared Components

### 1. `<PageShell>` — Consistent Page Wrapper

**File:** `packages/SimpleModule.UI/components/page-shell.tsx`

```tsx
interface Breadcrumb {
  label: string;
  href?: string;  // omit for current page
}

interface PageShellProps {
  title: string;
  description?: string;
  actions?: React.ReactNode;
  breadcrumbs?: Breadcrumb[];
  children: React.ReactNode;
  className?: string;
  maxWidth?: string;
}
```

Composes: Container + optional Breadcrumbs + PageHeader (with mb-0 baked in) + children.

### 2. `<DataGridPage>` — Consistent List Page Layout

**File:** `packages/SimpleModule.UI/components/data-grid-page.tsx`

```tsx
interface DataGridPageProps<T> {
  // PageShell props (passed through)
  title: string;
  description?: string;
  actions?: React.ReactNode;
  breadcrumbs?: Breadcrumb[];
  maxWidth?: string;

  // Filter bar (rendered between header and grid)
  filterBar?: React.ReactNode;

  // Data + grid
  data: T[];
  pageSize?: number;
  pageSizeOptions?: number[];
  children: (pageData: T[]) => React.ReactNode;

  // Empty state (built-in with sensible defaults)
  emptyTitle?: string;
  emptyDescription?: string;
  emptyIcon?: React.ReactNode;
  emptyAction?: React.ReactNode;
}
```

Composes: PageShell + optional filterBar + built-in empty state (when data is empty) + DataGrid.

## Migration Scope

| Page | Current | Target |
|------|---------|--------|
| Products Browse | Manual Container | PageShell |
| Products Manage | Manual assembly | DataGridPage |
| Admin Users | Custom pagination | DataGridPage |
| Admin Roles | Manual assembly | DataGridPage |
| Orders List | Manual assembly | DataGridPage |
| OpenIddict Clients | Manual assembly | DataGridPage |
| AuditLogs Browse | Manual assembly | DataGridPage + filterBar |
| AuditLogs Dashboard | Manual Container | PageShell |
| AuditLogs Detail | Manual Container | PageShell + breadcrumbs |
| PageBuilder PagesList | Manual h1 | PageShell |
| PageBuilder Manage | Manual h1 | DataGridPage |
| Settings AdminSettings | Manual h1 | PageShell |
| Settings MenuManager | Manual breadcrumb+header | PageShell + breadcrumbs |
| Dashboard Home | Manual Container | PageShell |
