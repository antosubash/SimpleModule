import { router } from '@inertiajs/react';
import { useState } from 'react';
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
import type { PageSummary } from '../types';

interface Props {
  pages: PageSummary[];
}

export default function Manage({ pages }: Props) {
  const [tagInputs, setTagInputs] = useState<Record<number, string>>({});

  function handleDelete(id: number, title: string) {
    if (!confirm(`Delete page "${title}"?`)) return;
    fetch(`/api/pagebuilder/${id}`, { method: 'DELETE' }).then(() => router.reload());
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
    <div>
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1 className="text-2xl font-extrabold tracking-tight">Pages</h1>
          <p className="text-text-muted text-sm mt-1">Manage content pages</p>
        </div>
        <Button onClick={() => router.get('/admin/pages/new')}>New Page</Button>
      </div>

      {pages.length === 0 ? (
        <p className="text-text-muted text-sm">
          No pages yet. Create your first page to get started.
        </p>
      ) : (
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
            {pages.map((page) => (
              <TableRow key={page.id}>
                <TableCell className="font-medium">{page.title}</TableCell>
                <TableCell className="text-text-secondary">/p/{page.slug}</TableCell>
                <TableCell>
                  <div className="flex gap-1.5">
                    <Badge variant={page.isPublished ? 'success' : 'secondary'}>
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
                      />
                    </form>
                  </div>
                </TableCell>
                <TableCell className="text-sm text-text-muted">
                  {new Date(page.updatedAt).toLocaleDateString()}
                </TableCell>
                <TableCell>
                  <div className="flex gap-2 flex-wrap">
                    {page.isPublished && (
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => window.open(`/p/${page.slug}`, '_blank')}
                      >
                        View
                      </Button>
                    )}
                    {page.hasDraft && (
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => window.open(`/p/${page.slug}/draft`, '_blank')}
                      >
                        View Draft
                      </Button>
                    )}
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => router.get(`/admin/pages/${page.id}/edit`)}
                    >
                      Edit
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleTogglePublish(page.id, page.isPublished)}
                    >
                      {page.isPublished ? 'Unpublish' : 'Publish'}
                    </Button>
                    <Button
                      variant="danger"
                      size="sm"
                      onClick={() => handleDelete(page.id, page.title)}
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
    </div>
  );
}
