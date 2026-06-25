import { router } from '@inertiajs/react';
import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Badge,
  Button,
  Card,
  CardContent,
  EmptyState,
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
import type { EmailMessage } from '../types';
import { HistoryFilters } from './components/HistoryFilters';
import { HistoryPagination } from './components/HistoryPagination';

type EmailStatus = 'Queued' | 'Sending' | 'Sent' | 'Failed' | 'Retrying';

function statusVariant(status: EmailStatus): 'default' | 'success' | 'danger' | 'warning' | 'info' {
  switch (status) {
    case 'Sent':
      return 'success';
    case 'Failed':
      return 'danger';
    case 'Sending':
    case 'Retrying':
      return 'warning';
    default:
      return 'default';
  }
}

interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

interface Filters {
  status?: string;
  to?: string;
  subject?: string;
  dateFrom?: string;
  dateTo?: string;
  before?: string;
}

interface Props {
  result: PagedResult<EmailMessage>;
  filters: Filters;
}

// Cursor trail persisted across Inertia navigations within the browser session, so
// "Previous" can return to an already-visited cursor.
const CURSOR_STACK_KEY = 'email-history-cursors';
function readCursorStack(): string[] {
  if (typeof window === 'undefined') return [];
  try {
    const raw = window.sessionStorage.getItem(CURSOR_STACK_KEY);
    return raw ? (JSON.parse(raw) as string[]) : [];
  } catch {
    return [];
  }
}
function writeCursorStack(stack: string[]): void {
  if (typeof window !== 'undefined') {
    window.sessionStorage.setItem(CURSOR_STACK_KEY, JSON.stringify(stack));
  }
}

function buildFilterParams(f: Filters, before?: string): Record<string, string> {
  const params: Record<string, string> = {};
  if (f.status) params.status = f.status;
  if (f.to) params.to = f.to;
  if (f.subject) params.subject = f.subject;
  if (f.dateFrom) params.dateFrom = f.dateFrom;
  if (f.dateTo) params.dateTo = f.dateTo;
  // Keyset cursor: fetch the page of messages older than `before`.
  if (before) params.before = before;
  return params;
}

export default function History({ result, filters }: Props) {
  const { t } = useTranslation('Email');
  const [status, setStatus] = useState(filters.status ?? '__all__');
  const [to, setTo] = useState(filters.to ?? '');
  const [subject, setSubject] = useState(filters.subject ?? '');
  const [dateFrom, setDateFrom] = useState(filters.dateFrom ?? '');
  const [dateTo, setDateTo] = useState(filters.dateTo ?? '');

  const before = filters.before ? String(filters.before) : undefined;
  const isFirstPage = !before;
  // Total is only computed for the first page (offset); keyset pages report -1.
  const knownTotal = result.totalCount >= 0;
  const items = result.items;
  const canNext = items.length >= result.pageSize;
  const nextCursor = items.length > 0 ? String(items[items.length - 1].createdAt) : undefined;

  function currentFilters(): Filters {
    return {
      status: status !== '__all__' ? status : undefined,
      to: to || undefined,
      subject: subject || undefined,
      dateFrom: dateFrom || undefined,
      dateTo: dateTo || undefined,
    };
  }

  function navigateNewest(f: Filters) {
    writeCursorStack([]);
    router.get('/email/history', buildFilterParams(f));
  }

  function applyFilters(e?: FormEvent) {
    e?.preventDefault();
    navigateNewest(currentFilters());
  }

  function clearFilters() {
    writeCursorStack([]);
    router.get('/email/history');
  }

  function goNext() {
    if (!nextCursor) return;
    const stack = readCursorStack();
    stack.push(before ?? ''); // remember current cursor ('' marks the first page)
    writeCursorStack(stack);
    router.get('/email/history', buildFilterParams(currentFilters(), nextCursor), {
      preserveScroll: true,
    });
  }

  function goPrev() {
    const stack = readCursorStack();
    const prev = stack.pop();
    writeCursorStack(stack);
    const target = prev && prev.length > 0 ? prev : undefined;
    router.get('/email/history', buildFilterParams(currentFilters(), target), {
      preserveScroll: true,
    });
  }

  function goNewest() {
    navigateNewest(currentFilters());
  }

  const hasActiveFilters = Boolean(status !== '__all__' || to || subject || dateFrom || dateTo);

  return (
    <PageShell
      className="space-y-4 sm:space-y-6"
      title={t(EmailKeys.History.Title)}
      description={t(EmailKeys.History.Description)}
    >
      <HistoryFilters
        status={status}
        to={to}
        subject={subject}
        dateFrom={dateFrom}
        dateTo={dateTo}
        hasActiveFilters={hasActiveFilters}
        onStatusChange={setStatus}
        onToChange={setTo}
        onSubjectChange={setSubject}
        onDateFromChange={setDateFrom}
        onDateToChange={setDateTo}
        onApplyFilters={applyFilters}
        onClearFilters={clearFilters}
      />

      {items.length === 0 ? (
        <Card>
          <CardContent>
            <EmptyState
              icon={
                <svg
                  aria-hidden="true"
                  width="22"
                  height="22"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="1.6"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                >
                  <path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z" />
                  <polyline points="22,6 12,13 2,6" />
                </svg>
              }
              title={t(EmailKeys.History.EmptyTitle)}
              description={
                hasActiveFilters
                  ? t(EmailKeys.History.EmptyWithFilters)
                  : t(EmailKeys.History.EmptyDescription)
              }
              secondaryAction={
                hasActiveFilters ? (
                  <Button variant="secondary" onClick={clearFilters}>
                    {t(EmailKeys.History.FilterClear)}
                  </Button>
                ) : undefined
              }
            />
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
                  {items.map((m) => (
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
                      <TableCell className="max-w-[200px] truncate text-danger">
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

      {items.length > 0 && (
        <div className="flex flex-col items-center gap-2 sm:flex-row sm:justify-between">
          <span className="text-sm text-text-muted">
            {knownTotal
              ? `${t(EmailKeys.History.Showing)} ${result.totalCount.toLocaleString()}`
              : null}
          </span>
          <HistoryPagination
            canPrev={!isFirstPage}
            canNext={canNext}
            showNewest={!isFirstPage}
            newestLabel={t(EmailKeys.History.Newest)}
            prevLabel={t(EmailKeys.History.Previous)}
            nextLabel={t(EmailKeys.History.Next)}
            onPrev={goPrev}
            onNext={goNext}
            onNewest={goNewest}
          />
        </div>
      )}
    </PageShell>
  );
}
