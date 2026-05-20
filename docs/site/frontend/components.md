---
outline: deep
---

# UI Components

The `@simplemodule/ui` package provides a comprehensive set of React components built on [Radix UI](https://www.radix-ui.com/) primitives with Tailwind CSS styling. It follows the shadcn/ui approach -- components live in your workspace as source code, not as an opaque npm dependency.

## Package Overview

The package is located at `packages/SimpleModule.UI/` and uses two export paths:

```ts
// Import components
import { Button, Card, Dialog } from '@simplemodule/ui';

// Import utilities
import { cn } from '@simplemodule/ui/lib/utils';
```

## The `cn` Utility

The `cn` function combines `clsx` and `tailwind-merge` to safely merge Tailwind classes:

```ts
import { cn } from '@simplemodule/ui/lib/utils';

<div className={cn('p-4 bg-surface', isActive && 'bg-primary-subtle', className)} />
```

This avoids class conflicts (e.g., `p-4` and `p-2` don't both end up in the DOM -- `tailwind-merge` resolves them).

## Available Components

### Layout

| Component | Description |
|---|---|
| `Container` | Centered max-width container with variant sizes |
| `Grid` | CSS grid wrapper with configurable columns |
| `Stack` | Flexbox stack (vertical/horizontal) with gap control |
| `PageShell` | Full page wrapper with title, description, and breadcrumbs |
| `PageHeader` | Page header with title and actions slot |
| `Section` | Semantic section wrapper |
| `Separator` | Visual divider (horizontal or vertical) |
| `AspectRatio` | Maintains a fixed aspect ratio for content |
| `ScrollArea` | Custom scrollbar container |
| `Resizable` | Resizable panel layout (`ResizablePanel`, `ResizableHandle`, `ResizablePanelGroup`) |

### Data Display

| Component | Description |
|---|---|
| `Card` | Surface container (`Card`, `CardHeader`, `CardContent`, `CardFooter`, `CardTitle`, `CardDescription`) |
| `Table` | Styled table (`Table`, `TableHeader`, `TableBody`, `TableRow`, `TableHead`, `TableCell`) |
| `DataGrid` | Feature-rich data table with sorting, filtering, and pagination |
| `DataGridPage` | Full-page data grid with built-in search and actions |
| `Badge` | Inline status indicator with variants |
| `Avatar` | User avatar with image and fallback |
| `HoverCard` | Card shown on hover |
| `Tooltip` | Contextual tooltip |
| `Calendar` | Date calendar display |
| `Chart` | Recharts wrapper with theming (`ChartContainer`, `ChartTooltip`, `ChartLegend`) |
| `Skeleton` | Loading placeholder |
| `Spinner` | Loading spinner with size variants |
| `Progress` | Progress bar |

### Navigation

| Component | Description |
|---|---|
| `Breadcrumb` | Breadcrumb navigation (`Breadcrumb`, `BreadcrumbList`, `BreadcrumbItem`, `BreadcrumbLink`, `BreadcrumbPage`, `BreadcrumbSeparator`) |
| `Tabs` | Tab navigation (`Tabs`, `TabsList`, `TabsTrigger`, `TabsContent`) |
| `Sidebar` | Application sidebar (`Sidebar`, `SidebarHeader`, `SidebarContent`, `SidebarMenu`, `SidebarMenuItem`, etc.) |
| `DropdownMenu` | Dropdown menu (`DropdownMenu`, `DropdownMenuTrigger`, `DropdownMenuContent`, `DropdownMenuItem`, etc.) |
| `Command` | Command palette / search (`Command`, `CommandInput`, `CommandList`, `CommandItem`, etc.) |
| `Popover` | Floating content panel |

### Forms

| Component | Description |
|---|---|
| `Button` | Button with variants (`primary`, `secondary`, `ghost`, `danger`, `outline`) and sizes (`sm`, `default`, `lg`) |
| `Input` | Text input with variants |
| `Textarea` | Multi-line text input |
| `Checkbox` | Checkbox input |
| `RadioGroup` | Radio button group (`RadioGroup`, `RadioGroupItem`) |
| `Select` | Dropdown select (`Select`, `SelectTrigger`, `SelectValue`, `SelectContent`, `SelectItem`) |
| `Switch` | Toggle switch |
| `Slider` | Range slider |
| `Label` | Form label |
| `Field` | Form field wrapper with label, description, and error (`Field`, `FieldGroup`, `FieldDescription`, `FieldError`) |
| `DatePicker` | Date picker combining calendar and popover |
| `Toggle` | Toggle button |
| `ToggleGroup` | Group of toggle buttons |

### Feedback

| Component | Description |
|---|---|
| `Alert` | Alert message with variants (`Alert`, `AlertTitle`, `AlertDescription`) |
| `Dialog` | Modal dialog (`Dialog`, `DialogTrigger`, `DialogContent`, `DialogHeader`, `DialogTitle`, `DialogDescription`, `DialogFooter`) |
| `Sheet` | Slide-out panel (`Sheet`, `SheetTrigger`, `SheetContent`, `SheetHeader`, `SheetTitle`, `SheetDescription`) |
| `Toast` | Toast notification (`Toast`, `ToastProvider`, `ToastViewport`, `ToastTitle`, `ToastDescription`, `ToastAction`) |
| `Accordion` | Collapsible content sections (`Accordion`, `AccordionItem`, `AccordionTrigger`, `AccordionContent`) |
| `Collapsible` | Single collapsible section |

## Usage Examples

### Page with Cards

```tsx
import { Card, CardContent, PageShell } from '@simplemodule/ui';
import type { Customer } from '../types';

export default function Browse({ customers }: { customers: Customer[] }) {
  return (
    <PageShell title="Customers" description="Browse the customer list.">
      <div className="space-y-3">
        {customers.map((c) => (
          <Card key={c.id}>
            <CardContent className="flex justify-between items-center">
              <span className="font-medium">{c.name}</span>
              <span className="text-text-muted">{c.email}</span>
            </CardContent>
          </Card>
        ))}
      </div>
    </PageShell>
  );
}
```

### Form with Field Components

```tsx
import { Button, Field, FieldError, FieldGroup, Input, Label } from '@simplemodule/ui';

function CreateForm() {
  return (
    <form>
      <FieldGroup>
        <Field>
          <Label>Customer Name</Label>
          <Input name="name" placeholder="Enter customer name" />
          <FieldError>Name is required</FieldError>
        </Field>
        <Button type="submit">Create Customer</Button>
      </FieldGroup>
    </form>
  );
}
```

### Button Variants

```tsx
import { Button } from '@simplemodule/ui';

<Button>Save</Button>                          {/* variant="primary" by default */}
<Button variant="secondary">Cancel</Button>
<Button variant="ghost">Dismiss</Button>
<Button variant="danger">Delete</Button>
<Button variant="outline">Learn more</Button>

<Button size="sm">Compact</Button>
<Button size="lg">Hero CTA</Button>
```

`asChild` forwards the variant styles to a child element (useful with `<Link>`):

```tsx
import { Link } from '@inertiajs/react';

<Button asChild>
  <Link href="/customers/new">New customer</Link>
</Button>
```

### Data Grid Page

```tsx
import { DataGridPage } from '@simplemodule/ui';

export default function Manage({ customers }) {
  return (
    <DataGridPage
      title="Customers"
      description="Manage your customer list."
      columns={columns}
      data={customers}
    />
  );
}
```

## Dependencies

The UI package relies on these key dependencies:

- **Radix UI** -- Accessible, unstyled primitives (dialog, dropdown, select, tabs, etc.)
- **class-variance-authority (CVA)** -- Type-safe component variants
- **clsx + tailwind-merge** -- Class name composition
- **cmdk** -- Command palette
- **react-day-picker** -- Calendar and date picker
- **recharts** -- Charting library

React and React-DOM are peer dependencies -- they are provided by the host application, not bundled.

## Next Steps

- [Styling & Theming](/frontend/styling) -- Tailwind CSS configuration and theme customization
- [Vite Build System](/frontend/vite) -- how module bundles are built and served
- [Pages Registry](/frontend/pages) -- how components are registered and resolved
