import { router } from '@inertiajs/react';
import {
  Badge,
  Button,
  DataGrid,
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

interface Props {
  pages: PageSummary[];
}

export default function Manage({ pages }: Props) {
  const [tagInputs, setTagInputs] = useState<Record<number, string>>({});
  const [deleteTarget, setDeleteTarget] = useState<{
    id: number;
    title: string;
  } | null>(null);

  function handleDelete() {
    if (!deleteTarget) return;
    fetch(`/api/pagebuilder/${deleteTarget.id}`, { method: 'DELETE' }).then(() => {
      setDeleteTarget(null);
      router.reload();
    });
  }

  function handleTogglePublish(id: number, isPublished: boolean) {
    const endpoint = isPublished ? 'unpublish' : 'publish';
    fetch(`/api/pagebuilder/${id}/${endpoint}`, { method: 'POST' }).then(() => router.reload());
  }

  function handleAddTag(pageId: number) {
    const name = tagInputs[pageId]?.trim();
    if (!name) return;
    fetch(`/api/pagebuilder/${pageId}/tags`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name }),
    }).then(() => {
      setTagInputs((prev) => ({ ...prev, [pageId]: '' }));
      router.reload();
    });
  }

  function handleRemoveTag(pageId: number, tagName: string) {
    fetch('/api/pagebuilder/tags')
      .then((r) => r.json())
      .then((tags: { id: number; name: string }[]) => {
        const tag = tags.find((t) => t.name === tagName);
        if (tag) {
          fetch(`/api/pagebuilder/${pageId}/tags/${tag.id}`, { method: 'DELETE' }).then(() =>
            router.reload(),
          );
        }
      });
  }

  return (
    <div className="mx-auto max-w-5xl space-y-6">
      <div className="flex justify-between items-center">
        <div className="space-y-1">
          <h1 className="text-2xl font-bold tracking-tight">Pages</h1>
          <p className="text-sm text-muted-foreground">Manage content pages</p>
        </div>
        <Button onClick={() => router.get('/admin/pages/new')}>New Page</Button>
      </div>

      {pages.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-12 text-center">
          <svg
            className="mb-4 h-12 w-12 text-muted-foreground/50"
            fill="none"
            stroke="currentColor"
            strokeWidth="1.5"
            viewBox="0 0 24 24"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z"
            />
          </svg>
          <h3 className="text-sm font-medium text-foreground">No pages yet</h3>
          <p className="mt-1 text-sm text-muted-foreground">
            Get started by creating your first content page.
          </p>
          <Button size="sm" className="mt-4" onClick={() => router.get('/admin/pages/new')}>
            New Page
          </Button>
        </div>
      ) : (
        <DataGrid data={pages}>
          {(pageData) => (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Title</TableHead>
                  <TableHead>Slug</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Tags</TableHead>
                  <TableHead>Updated</TableHead>
                  <TableHead />
                </TableRow>
              </TableHeader>
              <TableBody>
                {pageData.map((page) => (
                  <TableRow key={page.id}>
                    <TableCell className="font-medium">{page.title}</TableCell>
                    <TableCell className="text-text-secondary">/p/{page.slug}</TableCell>
                    <TableCell>
                      <div className="flex gap-1.5">
                        <Badge variant={page.isPublished ? 'success' : 'secondary'} data-testid="status-badge">
                          {page.isPublished ? 'Published' : 'Unpublished'}
                        </Badge>
                        {page.hasDraft && <Badge variant="warning">Draft</Badge>}
                      </div>
                    </TableCell>
                    <TableCell>
                      <div className="flex gap-1 flex-wrap items-center">
                        {page.tags.map((tag) => (
                          <Badge
                            key={tag}
                            variant="outline"
                            className="cursor-pointer hover:line-through"
                            onClick={() => handleRemoveTag(page.id, tag)}
                          >
                            {tag} &times;
                          </Badge>
                        ))}
                        <form
                          className="inline-flex gap-1"
                          onSubmit={(e) => {
                            e.preventDefault();
                            handleAddTag(page.id);
                          }}
                        >
                          <Input
                            placeholder="add tag"
                            className="h-6 w-20 text-xs px-1.5"
                            value={tagInputs[page.id] ?? ''}
                            onChange={(e) =>
                              setTagInputs((prev) => ({ ...prev, [page.id]: e.target.value }))
                            }
                            aria-label={`Add tag to ${page.title}`}
                          />
                        </form>
                      </div>
                    </TableCell>
                    <TableCell className="text-sm text-text-muted">
                      {new Date(page.updatedAt).toLocaleDateString()}
                    </TableCell>
                    <TableCell>
                      <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                          <Button variant="ghost" size="sm" aria-label={`Actions for ${page.title}`}>
                            <svg
                              width="16"
                              height="16"
                              fill="none"
                              stroke="currentColor"
                              strokeWidth="2"
                              viewBox="0 0 24 24"
                              aria-hidden="true"
                            >
                              <circle cx="12" cy="5" r="1" />
                              <circle cx="12" cy="12" r="1" />
                              <circle cx="12" cy="19" r="1" />
                            </svg>
                            <span className="sr-only">Actions</span>
                          </Button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent align="end">
                          <DropdownMenuItem onClick={() => router.get(`/admin/pages/${page.id}/edit`)}>
                            Edit
                          </DropdownMenuItem>
                          {page.isPublished && (
                            <DropdownMenuItem onClick={() => window.open(`/p/${page.slug}`, '_blank')}>
                              View Page
                            </DropdownMenuItem>
                          )}
                          {page.hasDraft && (
                            <DropdownMenuItem
                              onClick={() => window.open(`/p/${page.slug}/draft`, '_blank')}
                            >
                              Preview Draft
                            </DropdownMenuItem>
                          )}
                          <DropdownMenuSeparator />
                          <DropdownMenuItem
                            onClick={() => handleTogglePublish(page.id, page.isPublished)}
                          >
                            {page.isPublished ? 'Unpublish' : 'Publish'}
                          </DropdownMenuItem>
                          <DropdownMenuSeparator />
                          <DropdownMenuItem
                            className="text-danger"
                            onClick={() => setDeleteTarget({ id: page.id, title: page.title })}
                          >
                            Delete
                          </DropdownMenuItem>
                        </DropdownMenuContent>
                      </DropdownMenu>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </DataGrid>
      )}

      <Dialog open={deleteTarget !== null} onOpenChange={(open) => !open && setDeleteTarget(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete Page</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete &ldquo;{deleteTarget?.title}&rdquo;? This page will be
              permanently removed.
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
    </div>
  );
}
