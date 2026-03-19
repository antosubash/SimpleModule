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
import type { PageSummary } from '../types';

interface Props {
  pages: PageSummary[];
}

export default function Manage({ pages }: Props) {
  function handleDelete(id: number, title: string) {
    if (!confirm(`Delete page "${title}"?`)) return;
    fetch(`/api/pagebuilder/${id}`, { method: 'DELETE' }).then(() => router.reload());
  }

  function handleTogglePublish(id: number, isPublished: boolean) {
    const endpoint = isPublished ? 'unpublish' : 'publish';
    fetch(`/api/pagebuilder/${id}/${endpoint}`, { method: 'POST' }).then(() => router.reload());
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
                    {page.hasDraft && (
                      <Badge variant="warning">Draft</Badge>
                    )}
                  </div>
                </TableCell>
                <TableCell>
                  <div className="flex gap-1 flex-wrap">
                    {page.tags.map((tag) => (
                      <Badge key={tag} variant="outline">{tag}</Badge>
                    ))}
                  </div>
                </TableCell>
                <TableCell className="text-sm text-text-muted">
                  {new Date(page.updatedAt).toLocaleDateString()}
                </TableCell>
                <TableCell>
                  <div className="flex gap-3">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => router.get(`/admin/pages/${page.id}/edit`)}
                    >
                      Edit
                    </Button>
                    {page.hasDraft && (
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => window.open(`/p/${page.slug}/draft`, '_blank')}
                      >
                        Preview Draft
                      </Button>
                    )}
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
