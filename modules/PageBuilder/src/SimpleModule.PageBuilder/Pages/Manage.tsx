import { router } from '@inertiajs/react';
import { useTranslation } from '@simplemodule/client/use-translation';
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
import { PageBuilderKeys } from '@/Locales/keys';
import type { PageSummary } from '@/types';

interface Props {
  pages: PageSummary[];
}

export default function Manage({ pages }: Props) {
  const { t } = useTranslation('PageBuilder');
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
        title={t(PageBuilderKeys.Manage.Title)}
        description={t(PageBuilderKeys.Manage.Description)}
        actions={
          <Button onClick={() => router.get('/pages/new')}>
            {t(PageBuilderKeys.Manage.NewPage)}
          </Button>
        }
        data={pages}
        emptyTitle={t(PageBuilderKeys.Manage.EmptyTitle)}
        emptyDescription={t(PageBuilderKeys.Manage.EmptyDescription)}
        emptyAction={
          <Button size="sm" onClick={() => router.get('/pages/new')}>
            {t(PageBuilderKeys.Manage.NewPage)}
          </Button>
        }
      >
        {(pageData) => (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t(PageBuilderKeys.Manage.Table.Title)}</TableHead>
                <TableHead>{t(PageBuilderKeys.Manage.Table.Slug)}</TableHead>
                <TableHead>{t(PageBuilderKeys.Manage.Table.Status)}</TableHead>
                <TableHead>{t(PageBuilderKeys.Manage.Table.Tags)}</TableHead>
                <TableHead>{t(PageBuilderKeys.Manage.Table.Updated)}</TableHead>
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
                        variant={page.isPublished ? 'success' : 'default'}
                        data-testid="status-badge"
                      >
                        {page.isPublished
                          ? t(PageBuilderKeys.Manage.Status.Published)
                          : t(PageBuilderKeys.Manage.Status.Unpublished)}
                      </Badge>
                      {page.hasDraft && (
                        <Badge variant="warning">{t(PageBuilderKeys.Manage.Status.Draft)}</Badge>
                      )}
                    </div>
                  </TableCell>
                  <TableCell>
                    <div className="flex gap-1 flex-wrap items-center">
                      {page.tags.map((tag) => (
                        <Badge
                          key={tag}
                          variant="info"
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
                          placeholder={t(PageBuilderKeys.Manage.Tag.AddPlaceholder)}
                          className="h-6 w-20 text-xs px-1.5"
                          value={tagInputs[page.id] ?? ''}
                          onChange={(e) =>
                            setTagInputs((prev) => ({ ...prev, [page.id]: e.target.value }))
                          }
                          aria-label={t(PageBuilderKeys.Manage.Tag.AddAriaLabel, {
                            title: page.title,
                          })}
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
                        <Button
                          variant="ghost"
                          size="sm"
                          aria-label={t(PageBuilderKeys.Manage.Actions.AriaLabel, {
                            title: page.title,
                          })}
                        >
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
                          <span className="sr-only">
                            {t(PageBuilderKeys.Manage.Actions.SrOnly)}
                          </span>
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end">
                        <DropdownMenuItem onClick={() => router.get(`/pages/${page.id}/edit`)}>
                          {t(PageBuilderKeys.Manage.Actions.Edit)}
                        </DropdownMenuItem>
                        {page.isPublished && (
                          <DropdownMenuItem
                            onClick={() => window.open(`/pages/view/${page.slug}`, '_blank')}
                          >
                            {t(PageBuilderKeys.Manage.Actions.ViewPage)}
                          </DropdownMenuItem>
                        )}
                        {page.hasDraft && (
                          <DropdownMenuItem
                            onClick={() => window.open(`/pages/view/${page.slug}/draft`, '_blank')}
                          >
                            {t(PageBuilderKeys.Manage.Actions.PreviewDraft)}
                          </DropdownMenuItem>
                        )}
                        <DropdownMenuSeparator />
                        <DropdownMenuItem
                          onClick={() => handleTogglePublish(page.id, page.isPublished)}
                        >
                          {page.isPublished
                            ? t(PageBuilderKeys.Manage.Actions.Unpublish)
                            : t(PageBuilderKeys.Manage.Actions.Publish)}
                        </DropdownMenuItem>
                        <DropdownMenuSeparator />
                        <DropdownMenuItem
                          className="text-danger"
                          onClick={() => setDeleteTarget({ id: page.id, title: page.title })}
                        >
                          {t(PageBuilderKeys.Manage.Actions.Delete)}
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
            <DialogTitle>{t(PageBuilderKeys.Manage.DeleteDialog.Title)}</DialogTitle>
            <DialogDescription>
              {t(PageBuilderKeys.Manage.DeleteDialog.Description, {
                title: deleteTarget?.title ?? '',
              })}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="secondary" onClick={() => setDeleteTarget(null)}>
              {t(PageBuilderKeys.Manage.DeleteDialog.Cancel)}
            </Button>
            <Button variant="danger" onClick={handleDelete}>
              {t(PageBuilderKeys.Manage.DeleteDialog.Confirm)}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
