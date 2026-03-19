# Page Enhancements Design

## Features

### 1. Metadata (SEO fields)

Add to Page entity:
- `string? MetaDescription` (max 300)
- `string? MetaKeywords` (max 500)
- `string? OgImage` (URL string, max 500)

Stored on the entity, exposed via API. Manage page shows expandable SEO section.

### 2. Page Templates

New `PageTemplate` entity:
- `PageTemplateId Id` (strongly-typed, int-based)
- `string Name` (max 200, unique)
- `string Content` (Puck JSON)
- `DateTime CreatedAt`

Editor: "Save as Template" button saves current content as named template. "New Page" shows template picker.

API: `GET /api/pagebuilder/templates`, `POST /api/pagebuilder/templates`, `DELETE /api/pagebuilder/templates/{id}`

### 3. Tags

New `PageTag` entity:
- `PageTagId Id` (strongly-typed, int-based)
- `string Name` (max 100, unique, slugified)

Many-to-many join table `PagePageTag`. Tags shown as chips in Manage. Filter by tag support.

### 4. Soft Delete

Add `DateTime? DeletedAt` to Page. Delete sets timestamp instead of removing. Global query filter excludes soft-deleted pages.

New endpoints:
- `GET /api/pagebuilder/trash` — list deleted pages
- `POST /api/pagebuilder/{id}/restore` — clears DeletedAt
- `DELETE /api/pagebuilder/{id}/permanent` — hard delete

Manage page gets Trash view with Restore/Permanent Delete actions.

### 5. Custom Slug with Validation

Editable slug field in editor/manage UI. Validation: lowercase, alphanumeric + hyphens, 3-200 chars, unique. Returns 400 if invalid or taken. Pre-fills from title on new pages.
