import { router } from '@inertiajs/react';
import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Badge,
  Button,
  Card,
  CardContent,
  Input,
  PageShell,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';
import { type FormEvent, useState } from 'react';
import { EmailKeys } from '../Locales/keys';
import type { EmailMessage } from '../types';

type EmailStatus = 'Queued' | 'Sending' | 'Sent' | 'Failed' | 'Retrying';

function statusVariant(status: EmailStatus): 'default' | 'secondary' | 'destructive' | 'outline' {
  switch (status) {
    case 'Sent':
      return 'default';
    case 'Failed':
      return 'destructive';
    case 'Sending':
    case 'Retrying':
      return 'secondary';
    default:
      return 'outline';
  }
}

interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

interface Props {
  result: PagedResult<EmailMessage>;
  filters: {
    status?: string;
    to?: string;
    subject?: string;
    dateFrom?: string;
    dateTo?: string;
  };
}

const STATUS_OPTIONS: EmailStatus[] = ['Queued', 'Sending', 'Sent', 'Failed', 'Retrying'];

function buildFilterParams(f: Props['filters'], page?: number): Record<string, string> {
  const params: Record<string, string> = {};
  if (f.status) params.status = f.status;
  if (f.to) params.to = f.to;
  if (f.subject) params.subject = f.subject;
  if (f.dateFrom) params.dateFrom = f.dateFrom;
  if (f.dateTo) params.dateTo = f.dateTo;
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

export default function History({ result, filters }: Props) {
  const { t } = useTranslation('Email');
  const [status, setStatus] = useState(filters.status ?? '__all__');
  const [to, setTo] = useState(filters.to ?? '');
  const [subject, setSubject] = useState(filters.subject ?? '');
  const [dateFrom, setDateFrom] = useState(filters.dateFrom ?? '');
  const [dateTo, setDateTo] = useState(filters.dateTo ?? '');

  const totalPages = Math.max(1, Math.ceil(result.totalCount / result.pageSize));
  const currentPage = result.page;

  function currentFilters(): Props['filters'] {
    return {
      status: status !== '__all__' ? status : undefined,
      to: to || undefined,
      subject: subject || undefined,
      dateFrom: dateFrom || undefined,
      dateTo: dateTo || undefined,
    };
  }

  function applyFilters(e?: FormEvent) {
    e?.preventDefault();
    router.get('/email/history', buildFilterParams(currentFilters()));
  }

  function clearFilters() {
    router.get('/email/history');
  }

  function goToPage(page: number) {
    router.get('/email/history', buildFilterParams(currentFilters(), page), {
      preserveState: true,
    });
  }

  const hasActiveFilters = status !== '__all__' || to || subject || dateFrom || dateTo;

  const startItem = (currentPage - 1) * result.pageSize + 1;
  const endItem = Math.min(currentPage * result.pageSize, result.totalCount);

  return (
    <PageShell
      className="space-y-4 sm:space-y-6"
      title={t(EmailKeys.History.Title)}
      description={t(EmailKeys.History.Description)}
    >
      {/* Filter Panel */}
      <Card>
        <CardContent>
          <form onSubmit={applyFilters}>
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 sm:gap-4 md:grid-cols-3 lg:grid-cols-4">
              <div className="space-y-1">
                <span className="text-xs font-medium text-text-muted">
                  {t(EmailKeys.History.FilterStatus)}
                </span>
                <Select value={status} onValueChange={setStatus}>
                  <SelectTrigger aria-label={t(EmailKeys.History.FilterStatus)}>
                    <SelectValue placeholder={t(EmailKeys.History.AllStatuses)} />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="__all__">{t(EmailKeys.History.AllStatuses)}</SelectItem>
                    {STATUS_OPTIONS.map((s) => (
                      <SelectItem key={s} value={s}>
                        {s}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-1">
                <label htmlFor="filter-to" className="text-xs font-medium text-text-muted">
                  {t(EmailKeys.History.FilterTo)}
                </label>
                <Input
                  id="filter-to"
                  placeholder={t(EmailKeys.History.FilterTo)}
                  value={to}
                  onChange={(e) => setTo(e.target.value)}
                />
              </div>
              <div className="space-y-1">
                <label htmlFor="filter-subject" className="text-xs font-medium text-text-muted">
                  {t(EmailKeys.History.FilterSubject)}
                </label>
                <Input
                  id="filter-subject"
                  placeholder={t(EmailKeys.History.FilterSubject)}
                  value={subject}
                  onChange={(e) => setSubject(e.target.value)}
                />
              </div>
              <div className="space-y-1">
                <label htmlFor="filter-date-from" className="text-xs font-medium text-text-muted">
                  {t(EmailKeys.History.FilterDateFrom)}
                </label>
                <Input
                  id="filter-date-from"
                  type="date"
                  value={dateFrom}
                  onChange={(e) => setDateFrom(e.target.value)}
                />
              </div>
              <div className="space-y-1">
                <label htmlFor="filter-date-to" className="text-xs font-medium text-text-muted">
                  {t(EmailKeys.History.FilterDateTo)}
                </label>
                <Input
                  id="filter-date-to"
                  type="date"
                  value={dateTo}
                  onChange={(e) => setDateTo(e.target.value)}
                />
              </div>
              <div className="flex items-end gap-2">
                <Button type="submit">{t(EmailKeys.History.FilterApply)}</Button>
                {hasActiveFilters && (
                  <Button variant="ghost" onClick={clearFilters}>
                    {t(EmailKeys.History.FilterClear)}
                  </Button>
                )}
              </div>
            </div>
          </form>
        </CardContent>
      </Card>

      {/* Results Table */}
      {result.items.length === 0 ? (
        <Card>
          <CardContent className="py-12 text-center">
            <p className="text-lg font-medium text-text">{t(EmailKeys.History.EmptyTitle)}</p>
            <p className="mt-1 text-sm text-text-muted">
              {hasActiveFilters
                ? t(EmailKeys.History.EmptyWithFilters)
                : t(EmailKeys.History.EmptyDescription)}
            </p>
            {hasActiveFilters && (
              <Button variant="secondary" className="mt-4" onClick={clearFilters}>
                {t(EmailKeys.History.FilterClear)}
              </Button>
            )}
          </CardContent>
        </Card>
      ) : (
        <Card>
          <CardContent className="p-0">
            <div className="-mx-4 overflow-x-auto px-4 sm:mx-0 sm:px-0">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>{t(EmailKeys.History.ColTo)}</TableHead>
                    <TableHead>{t(EmailKeys.History.ColSubject)}</TableHead>
                    <TableHead>{t(EmailKeys.History.ColStatus)}</TableHead>
                    <TableHead>{t(EmailKeys.History.ColProvider)}</TableHead>
                    <TableHead>{t(EmailKeys.History.ColSentAt)}</TableHead>
                    <TableHead>{t(EmailKeys.History.ColError)}</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {result.items.map((m) => (
                    <TableRow key={m.id}>
                      <TableCell className="font-medium">{m.to}</TableCell>
                      <TableCell>{m.subject}</TableCell>
                      <TableCell>
                        <Badge variant={statusVariant(m.status)}>{m.status}</Badge>
                      </TableCell>
                      <TableCell className="text-text-muted">{m.provider ?? '-'}</TableCell>
                      <TableCell className="text-text-muted">
                        {m.sentAt ? new Date(m.sentAt).toLocaleString() : '-'}
                      </TableCell>
                      <TableCell className="max-w-[200px] truncate text-destructive">
                        {m.errorMessage ?? '-'}
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
            {t(EmailKeys.History.Showing)} {startItem}-{endItem} {t(EmailKeys.History.Of)}{' '}
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
