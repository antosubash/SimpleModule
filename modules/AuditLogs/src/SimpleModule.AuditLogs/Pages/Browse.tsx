import { router } from '@inertiajs/react';
import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Button,
  Card,
  CardContent,
  EmptyState,
  PageShell,
  TooltipProvider,
} from '@simplemodule/ui';
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

// Cursor trail persisted across Inertia (server-rendered) navigations within the
// same browser session, so "Previous" can return to an already-visited cursor.
const CURSOR_STACK_KEY = 'auditlogs-browse-cursors';
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

function buildFilterParams(f: Partial<AuditQueryRequest>, before?: string): Record<string, string> {
  const params: Record<string, string> = {};
  if (f.from) params.from = String(f.from);
  if (f.to) params.to = String(f.to);
  if (f.source != null) params.source = String(f.source);
  if (f.action != null) params.action = String(f.action);
  if (f.module) params.module = f.module;
  if (f.searchText) params.searchText = f.searchText;

  // Keyset cursor: fetch the page of entries older than `before`.
  if (before) params.before = before;
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

  const before = filters.before ? String(filters.before) : undefined;
  const isFirstPage = !before;
  // Total is only computed for the first page (offset); keyset pages report -1.
  const knownTotal = result.totalCount >= 0;
  const items = result.items;
  // A full page implies more (older) rows likely exist.
  const canNext = items.length >= result.pageSize;
  const nextCursor = items.length > 0 ? String(items[items.length - 1].timestamp) : undefined;

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

  // Any change of filter set resets the cursor trail and returns to the newest page.
  function navigateNewest(f: Partial<AuditQueryRequest>) {
    writeCursorStack([]);
    router.get('/audit-logs/browse', buildFilterParams(f));
  }

  function applyFilters(e?: FormEvent) {
    e?.preventDefault();
    navigateNewest(currentFilters());
  }

  function clearFilters() {
    writeCursorStack([]);
    router.get('/audit-logs/browse');
  }

  function applyDatePreset(hours: number) {
    const now = new Date();
    const past = new Date(now.getTime() - hours * 60 * 60 * 1000);
    navigateNewest({
      ...currentFilters(),
      from: past.toISOString().slice(0, 16),
      to: now.toISOString().slice(0, 16),
    });
  }

  function goNext() {
    if (!nextCursor) return;
    const stack = readCursorStack();
    stack.push(before ?? ''); // remember current cursor ('' marks the first page)
    writeCursorStack(stack);
    router.get('/audit-logs/browse', buildFilterParams(currentFilters(), nextCursor), {
      preserveScroll: true,
    });
  }

  function goPrev() {
    const stack = readCursorStack();
    const prev = stack.pop();
    writeCursorStack(stack);
    const target = prev && prev.length > 0 ? prev : undefined;
    router.get('/audit-logs/browse', buildFilterParams(currentFilters(), target), {
      preserveScroll: true,
    });
  }

  function goNewest() {
    navigateNewest(currentFilters());
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

  return (
    <TooltipProvider>
      <PageShell
        className="space-y-4 sm:space-y-6"
        title={t(AuditLogsKeys.Browse.Title)}
        description={
          knownTotal
            ? t(AuditLogsKeys.Browse.TotalEntries, {
                count: result.totalCount.toLocaleString(),
              })
            : undefined
        }
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
                    <circle cx="11" cy="11" r="8" />
                    <path d="m21 21-4.3-4.3" />
                  </svg>
                }
                title={t(AuditLogsKeys.Browse.EmptyTitle)}
                description={
                  hasActiveFilters
                    ? t(AuditLogsKeys.Browse.EmptyWithFilters)
                    : t(AuditLogsKeys.Browse.EmptyNoFilters)
                }
                secondaryAction={
                  hasActiveFilters ? (
                    <Button variant="secondary" onClick={clearFilters}>
                      {t(AuditLogsKeys.Browse.ClearFilters)}
                    </Button>
                  ) : undefined
                }
              />
            </CardContent>
          </Card>
        ) : (
          <Card>
            <CardContent className="p-0">
              <div className="overflow-x-auto -mx-4 px-4 sm:mx-0 sm:px-0">
                <BrowseResultsTable items={items} />
              </div>
            </CardContent>
          </Card>
        )}

        {items.length > 0 && (
          <div className="flex flex-col items-center gap-2 sm:flex-row sm:justify-between">
            <span className="text-sm text-text-muted">
              {knownTotal
                ? t(AuditLogsKeys.Browse.TotalEntries, {
                    count: result.totalCount.toLocaleString(),
                  })
                : null}
            </span>
            <BrowsePagination
              canPrev={!isFirstPage}
              canNext={canNext}
              showNewest={!isFirstPage}
              newestLabel={t(AuditLogsKeys.Browse.Newest)}
              prevLabel={t(AuditLogsKeys.Browse.PrevPage)}
              nextLabel={t(AuditLogsKeys.Browse.NextPage)}
              onPrev={goPrev}
              onNext={goNext}
              onNewest={goNewest}
            />
          </div>
        )}
      </PageShell>
    </TooltipProvider>
  );
}
