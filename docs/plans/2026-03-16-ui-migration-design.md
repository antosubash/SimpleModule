# UI Component Migration Design

## Goal

Migrate all module pages from native HTML elements + custom CSS classes to `@simplemodule/ui` components with default styling.

## Scope

12 pages across 4 modules:

- **Dashboard:** Home.tsx
- **Orders:** List.tsx, Create.tsx, Edit.tsx
- **Users:** Users.tsx, UsersEdit.tsx, Roles.tsx, RolesCreate.tsx, RolesEdit.tsx
- **Products:** Browse.tsx, Edit.tsx

Products/Manage.tsx and Products/Create.tsx are already migrated.

## Component Mapping

| Native HTML / Custom CSS | `@simplemodule/ui` Component |
|--------------------------|------------------------------|
| `<button className="btn-*">` | `<Button variant="...">` |
| `<input>` | `<Input>` |
| `<select>` | `<Select>` (Radix-based) |
| `<label>` | `<Label>` |
| `<textarea>` | `<Textarea>` |
| `<table>` markup | `Table`, `TableHeader`, `TableBody`, `TableRow`, `TableHead`, `TableCell` |
| `badge-*` spans/divs | `<Badge variant="...">` |
| `glass-card` / `panel` divs | `Card`, `CardHeader`, `CardContent` |
| Custom alert divs | `<Alert>` |
| `<input type="checkbox">` | `<Checkbox>` |
| `gradient-text` headings | Plain headings |

## Approach

- Migrate page by page, preserving all functionality (Inertia routing, form state, event handlers)
- Remove custom CSS classes (`btn-*`, `badge-*`, `glass-card`, `panel`, `gradient-text`)
- Adopt `@simplemodule/ui` default styling entirely
- Keep inline SVG icons as-is (no icon library in UI components)
