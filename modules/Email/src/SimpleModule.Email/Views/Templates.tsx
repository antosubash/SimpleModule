import { router } from '@inertiajs/react';
import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Button,
  Card,
  CardContent,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Input,
  PageShell,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';
import { type FormEvent, useState } from 'react';
import { EmailKeys } from '../Locales/keys';
import type { EmailTemplate } from '../types';

interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

interface Props {
  result: PagedResult<EmailTemplate>;
  filters: { search?: string };
}

function buildFilterParams(f: Props['filters'], page?: number): Record<string, string> {
  const params: Record<string, string> = {};
  if (f.search) params.search = f.search;
  if (page && page > 1) params.page = String(page);
  return params;
}

function ChevronLeft() {
  return (
    <svg
      className="h-4 w-4"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      viewBox="0 0 24 24"
      aria-hidden="true"
    >
      <path d="m15 18-6-6 6-6" />
    </svg>
  );
}

function ChevronRight() {
  return (
    <svg
      className="h-4 w-4"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      viewBox="0 0 24 24"
      aria-hidden="true"
    >
      <path d="m9 18 6-6-6-6" />
    </svg>
  );
}

export default function Templates({ result, filters }: Props) {
  const { t } = useTranslation('Email');
  const [search, setSearch] = useState(filters.search ?? '');
  const [deleteId, setDeleteId] = useState<number | null>(null);

  const totalPages = Math.max(1, Math.ceil(result.totalCount / result.pageSize));
  const currentPage = result.page;

  function currentFilters(): Props['filters'] {
    return { search: search || undefined };
  }

  function applySearch(e?: FormEvent) {
    e?.preventDefault();
    router.get('/email/templates', buildFilterParams(currentFilters()));
  }

  function goToPage(page: number) {
    router.get('/email/templates', buildFilterParams(currentFilters(), page), {
      preserveState: true,
    });
  }

  const startItem = (currentPage - 1) * result.pageSize + 1;
  const endItem = Math.min(currentPage * result.pageSize, result.totalCount);

  return (
    <>
      <PageShell
        className="space-y-4 sm:space-y-6"
        title={t(EmailKeys.Templates.Title)}
        description={t(EmailKeys.Templates.Description)}
        actions={
          <Button onClick={() => router.visit('/email/templates/create')}>
            {t(EmailKeys.Templates.NewTemplate)}
          </Button>
        }
      >
        {/* Search Bar */}
        <Card>
          <CardContent>
            <form onSubmit={applySearch} className="flex gap-3">
              <Input
                placeholder={t(EmailKeys.Templates.FilterSearch)}
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="max-w-sm"
              />
              <Button type="submit">{t(EmailKeys.History.FilterApply)}</Button>
            </form>
          </CardContent>
        </Card>

        {/* Results Table */}
        {result.items.length === 0 ? (
          <Card>
            <CardContent className="py-12 text-center">
              <p className="text-lg font-medium text-text">{t(EmailKeys.Templates.EmptyTitle)}</p>
              <p className="mt-1 text-sm text-text-muted">
                {t(EmailKeys.Templates.EmptyDescription)}
              </p>
            </CardContent>
          </Card>
        ) : (
          <Card>
            <CardContent className="p-0">
              <div className="-mx-4 overflow-x-auto px-4 sm:mx-0 sm:px-0">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>{t(EmailKeys.Templates.ColName)}</TableHead>
                      <TableHead>{t(EmailKeys.Templates.ColSlug)}</TableHead>
                      <TableHead>{t(EmailKeys.Templates.ColSubject)}</TableHead>
                      <TableHead>{t(EmailKeys.Templates.ColHtml)}</TableHead>
                      <TableHead className="w-[100px]">
                        {t(EmailKeys.Templates.ColActions)}
                      </TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {result.items.map((tpl) => (
                      <TableRow key={tpl.id}>
                        <TableCell className="font-medium">{tpl.name}</TableCell>
                        <TableCell className="text-text-muted">{tpl.slug}</TableCell>
                        <TableCell>{tpl.subject}</TableCell>
                        <TableCell>
                          {tpl.isHtml ? t(EmailKeys.Templates.Yes) : t(EmailKeys.Templates.No)}
                        </TableCell>
                        <TableCell>
                          <div className="flex gap-2">
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => router.visit(`/email/templates/${tpl.id}/edit`)}
                            >
                              {t(EmailKeys.Templates.EditButton)}
                            </Button>
                            <Button variant="danger" size="sm" onClick={() => setDeleteId(tpl.id)}>
                              {t(EmailKeys.Templates.DeleteButton)}
                            </Button>
                          </div>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            </CardContent>
          </Card>
        )}

        {/* Server-side Pagination */}
        {result.totalCount > 0 && (
          <div className="flex flex-col items-center gap-2 sm:flex-row sm:justify-between">
            <span className="text-sm text-text-muted">
              {t(EmailKeys.Templates.Showing)} {startItem}-{endItem} {t(EmailKeys.History.Of)}{' '}
              {result.totalCount.toLocaleString()}
            </span>
            {totalPages > 1 && (
              <div className="flex items-center gap-1">
                <Button
                  variant="ghost"
                  size="sm"
                  disabled={currentPage <= 1}
                  onClick={() => goToPage(currentPage - 1)}
                >
                  <ChevronLeft />
                </Button>
                {paginationRange(currentPage, totalPages).map((p) =>
                  p === 'ellipsis-start' || p === 'ellipsis-end' ? (
                    <span key={p} className="px-1 text-sm text-text-muted">
                      ...
                    </span>
                  ) : (
                    <Button
                      key={p}
                      variant={p === currentPage ? 'secondary' : 'ghost'}
                      size="sm"
                      className="h-8 w-8 p-0"
                      onClick={() => goToPage(p as number)}
                    >
                      {p}
                    </Button>
                  ),
                )}
                <Button
                  variant="ghost"
                  size="sm"
                  disabled={currentPage >= totalPages}
                  onClick={() => goToPage(currentPage + 1)}
                >
                  <ChevronRight />
                </Button>
              </div>
            )}
          </div>
        )}
      </PageShell>

      <Dialog open={deleteId !== null} onOpenChange={() => setDeleteId(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t(EmailKeys.Templates.DeleteDialog.Title)}</DialogTitle>
            <DialogDescription>{t(EmailKeys.Templates.DeleteDialog.Confirm)}</DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="secondary" onClick={() => setDeleteId(null)}>
              {t(EmailKeys.Templates.CancelButton)}
            </Button>
            <Button
              variant="danger"
              onClick={() => {
                if (deleteId !== null) {
                  router.delete(`/email/templates/${deleteId}`, {
                    onSuccess: () => setDeleteId(null),
                  });
                }
              }}
            >
              {t(EmailKeys.Templates.DeleteDialog.DeleteButton)}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}

/** Build a compact pagination range: 1 ... 4 5 [6] 7 8 ... 20 */
function paginationRange(
  current: number,
  total: number,
): (number | 'ellipsis-start' | 'ellipsis-end')[] {
  if (total <= 7) {
    return Array.from({ length: total }, (_, i) => i + 1);
  }
  const pages: (number | 'ellipsis-start' | 'ellipsis-end')[] = [];
  pages.push(1);
  if (current > 3) pages.push('ellipsis-start');
  const start = Math.max(2, current - 1);
  const end = Math.min(total - 1, current + 1);
  for (let i = start; i <= end; i++) pages.push(i);
  if (current < total - 2) pages.push('ellipsis-end');
  pages.push(total);
  return pages;
}
