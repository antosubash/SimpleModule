import { router } from '@inertiajs/react';
import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Badge,
  Button,
  Card,
  CardContent,
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

  const hasActiveFilters = Boolean(status !== '__all__' || to || subject || dateFrom || dateTo);

  const startItem = (currentPage - 1) * result.pageSize + 1;
  const endItem = Math.min(currentPage * result.pageSize, result.totalCount);

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

      {result.totalCount > 0 && (
        <div className="flex flex-col items-center gap-2 sm:flex-row sm:justify-between">
          <span className="text-sm text-text-muted">
            {t(EmailKeys.History.Showing)} {startItem}-{endItem} {t(EmailKeys.History.Of)}{' '}
            {result.totalCount.toLocaleString()}
          </span>
          <HistoryPagination
            currentPage={currentPage}
            totalPages={totalPages}
            onGoToPage={goToPage}
          />
        </div>
      )}
    </PageShell>
  );
}
