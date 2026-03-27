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
    <>
      <DataGridPage
        title="Pages"
        description="Manage content pages"
        actions={<Button onClick={() => router.get('/pages/new')}>New Page</Button>}
        data={pages}
        emptyTitle="No pages yet"
        emptyDescription="Get started by creating your first content page."
        emptyAction={
          <Button size="sm" onClick={() => router.get('/pages/new')}>
            New Page
          </Button>
        }
      >
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
                  <TableCell className="text-text-secondary">/pages/view/{page.slug}</TableCell>
                  <TableCell>
                    <div className="flex gap-1.5">
                      <Badge
                        variant={page.isPublished ? 'success' : 'secondary'}
                        data-testid="status-badge"
                      >
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
                        <DropdownMenuItem
                          onClick={() => router.get(`/pages/${page.id}/edit`)}
                        >
                          Edit
                        </DropdownMenuItem>
                        {page.isPublished && (
                          <DropdownMenuItem
                            onClick={() => window.open(`/pages/view/${page.slug}`, '_blank')}
                          >
                            View Page
                          </DropdownMenuItem>
                        )}
                        {page.hasDraft && (
                          <DropdownMenuItem
                            onClick={() => window.open(`/pages/view/${page.slug}/draft`, '_blank')}
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
      </DataGridPage>

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
    </>
  );
}
