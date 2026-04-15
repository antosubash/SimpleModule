import { router } from '@inertiajs/react';
import { useTranslation } from '@simplemodule/client/use-translation';
import { Button, Card, CardContent, PageShell, TooltipProvider } from '@simplemodule/ui';
import { type FormEvent, useState } from 'react';
import { AuditLogsKeys } from '@/Locales/keys';
import type { AuditEntry, AuditQueryRequest } from '@/types';
import { BrowseFilters } from './components/BrowseFilters';
import { BrowsePagination } from './components/BrowsePagination';
import { BrowseResultsTable } from './components/BrowseResultsTable';

interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

interface Props {
  result: PagedResult<AuditEntry>;
  filters: AuditQueryRequest;
}

function buildFilterParams(f: Partial<AuditQueryRequest>, page?: number): Record<string, string> {
  const params: Record<string, string> = {};
  if (f.from) params.from = String(f.from);
  if (f.to) params.to = String(f.to);
  if (f.source != null) params.source = String(f.source);
  if (f.action != null) params.action = String(f.action);
  if (f.module) params.module = f.module;
  if (f.searchText) params.searchText = f.searchText;

  if (page && page > 1) params.page = String(page);
  return params;
}

export default function Browse({ result, filters }: Props) {
  const { t } = useTranslation('AuditLogs');
  const [from, setFrom] = useState(filters.from ? String(filters.from) : '');
  const [to, setTo] = useState(filters.to ? String(filters.to) : '');
  const [source, setSource] = useState(filters.source != null ? String(filters.source) : '__all__');
  const [action, setAction] = useState(filters.action != null ? String(filters.action) : '__all__');
  const [module, setModule] = useState(filters.module ?? '');
  const [searchText, setSearchText] = useState(filters.searchText ?? '');

  const totalPages = Math.max(1, Math.ceil(result.totalCount / result.pageSize));
  const currentPage = result.page;

  function currentFilters() {
    return {
      from: from || undefined,
      to: to || undefined,
      source: source !== '__all__' ? Number(source) : undefined,
      action: action !== '__all__' ? Number(action) : undefined,
      module: module || undefined,
      searchText: searchText || undefined,
    };
  }

  function applyFilters(e?: FormEvent) {
    e?.preventDefault();
    router.get('/audit-logs/browse', buildFilterParams(currentFilters()));
  }

  function clearFilters() {
    router.get('/audit-logs/browse');
  }

  function applyDatePreset(hours: number) {
    const now = new Date();
    const past = new Date(now.getTime() - hours * 60 * 60 * 1000);
    const toLocal = now.toISOString().slice(0, 16);
    const fromLocal = past.toISOString().slice(0, 16);
    router.get(
      '/audit-logs/browse',
      buildFilterParams({
        ...currentFilters(),
        from: fromLocal,
        to: toLocal,
      }),
    );
  }

  function goToPage(page: number) {
    router.get('/audit-logs/browse', buildFilterParams(currentFilters(), page), {
      preserveState: true,
    });
  }

  function exportLogs(format: string) {
    const query = new URLSearchParams({
      ...buildFilterParams(currentFilters()),
      format,
    }).toString();
    window.location.href = `/api/audit-logs/export?${query}`;
  }

  const hasActiveFilters = Boolean(
    from || to || source !== '__all__' || action !== '__all__' || module || searchText,
  );

  const startItem = (currentPage - 1) * result.pageSize + 1;
  const endItem = Math.min(currentPage * result.pageSize, result.totalCount);

  return (
    <TooltipProvider>
      <PageShell
        className="space-y-4 sm:space-y-6"
        title={t(AuditLogsKeys.Browse.Title)}
        description={t(AuditLogsKeys.Browse.TotalEntries, {
          count: result.totalCount.toLocaleString(),
        })}
        actions={
          <div className="flex flex-col gap-2 sm:flex-row">
            <Button variant="secondary" onClick={() => exportLogs('csv')}>
              {t(AuditLogsKeys.Browse.ExportCsv)}
            </Button>
            <Button variant="secondary" onClick={() => exportLogs('json')}>
              {t(AuditLogsKeys.Browse.ExportJson)}
            </Button>
          </div>
        }
      >
        <BrowseFilters
          from={from}
          to={to}
          source={source}
          action={action}
          module={module}
          searchText={searchText}
          hasActiveFilters={hasActiveFilters}
          onFromChange={setFrom}
          onToChange={setTo}
          onSourceChange={setSource}
          onActionChange={setAction}
          onModuleChange={setModule}
          onSearchTextChange={setSearchText}
          onApplyFilters={applyFilters}
          onClearFilters={clearFilters}
          onApplyDatePreset={applyDatePreset}
        />

        {result.items.length === 0 ? (
          <Card>
            <CardContent className="py-12 text-center">
              <p className="text-lg font-medium text-text">{t(AuditLogsKeys.Browse.EmptyTitle)}</p>
              <p className="mt-1 text-sm text-text-muted">
                {hasActiveFilters
                  ? t(AuditLogsKeys.Browse.EmptyWithFilters)
                  : t(AuditLogsKeys.Browse.EmptyNoFilters)}
              </p>
              {hasActiveFilters && (
                <Button variant="secondary" className="mt-4" onClick={clearFilters}>
                  {t(AuditLogsKeys.Browse.ClearFilters)}
                </Button>
              )}
            </CardContent>
          </Card>
        ) : (
          <Card>
            <CardContent className="p-0">
              <div className="overflow-x-auto -mx-4 px-4 sm:mx-0 sm:px-0">
                <BrowseResultsTable items={result.items} />
              </div>
            </CardContent>
          </Card>
        )}

        {result.totalCount > 0 && (
          <div className="flex flex-col items-center gap-2 sm:flex-row sm:justify-between">
            <span className="text-sm text-text-muted">
              {t(AuditLogsKeys.Browse.Showing, {
                start: String(startItem),
                end: String(endItem),
                total: result.totalCount.toLocaleString(),
              })}
            </span>
            <BrowsePagination
              currentPage={currentPage}
              totalPages={totalPages}
              onGoToPage={goToPage}
            />
          </div>
        )}
      </PageShell>
    </TooltipProvider>
  );
}
