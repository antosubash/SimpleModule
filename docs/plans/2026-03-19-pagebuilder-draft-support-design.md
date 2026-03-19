# PageBuilder Draft Support Design

## Summary

Add a `DraftContent` column to the Page entity so editors can save work-in-progress content without affecting the live published version. Publishing copies draft to live content.

## Entity Changes

- Add `string? DraftContent` (nullable JSON) to `Page`
- Add `bool HasDraft` (computed) to `PageSummary`

## Content Flow

- **Editor saves** -> writes to `DraftContent` only
- **Publish** -> copies `DraftContent` to `Content`, sets `DraftContent = null`, sets `IsPublished = true`
- **Editor loads** -> sends `DraftContent ?? Content` to the editor
- **Unpublish** -> flips `IsPublished`, keeps both columns as-is

## Routes

| Route | Auth | Content Source |
|-------|------|----------------|
| `/p/{slug}` | Anonymous | `Content` (published) |
| `/p/{slug}/draft` | Admin role | `DraftContent` (draft preview) |

## Endpoint Changes

- `UpdateContentEndpoint` -> writes to `DraftContent` instead of `Content`
- `PublishEndpoint` -> copies `DraftContent` to `Content`, clears `DraftContent`
- `EditorEndpoint` -> sends `DraftContent ?? Content`
- New `ViewerDraftEndpoint` at `/p/{slug}/draft` (Admin role required)
- `ViewerEndpoint` -> unchanged (reads `Content`)

## Frontend Changes

- Viewer component receives `isDraft` prop; shows "Draft Preview" banner when true
- Manage page shows "Draft" badge next to pages with `hasDraft = true`
- Manage page shows "Preview Draft" link when draft exists

## Migration

- Add nullable `DraftContent TEXT` column to `PageBuilder_Pages` table
