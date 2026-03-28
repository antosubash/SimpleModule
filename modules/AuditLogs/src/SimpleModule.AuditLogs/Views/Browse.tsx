import { router } from '@inertiajs/react';
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
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@simplemodule/ui';
import { type FormEvent, useState } from 'react';
import type { AuditEntry, AuditQueryRequest } from '../types';

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

const SOURCE_LABELS: Record<number, string> = {
  0: 'HTTP',
  1: 'Domain',
  2: 'Changes',
};

const ACTION_LABELS: Record<number, string> = {
  0: 'Created',
  1: 'Updated',
  2: 'Deleted',
  3: 'Viewed',
  4: 'Login OK',
  5: 'Login Fail',
  6: 'Perm Granted',
  7: 'Perm Revoked',
  8: 'Setting Changed',
  9: 'Exported',
  10: 'Other',
};

const DATE_PRESETS = [
  { label: 'Last hour', hours: 1 },
  { label: 'Last 24h', hours: 24 },
  { label: 'Last 7 days', hours: 168 },
  { label: 'Last 30 days', hours: 720 },
];

function sourceBadgeVariant(source: number) {
  if (source === 1) return 'success' as const;
  if (source === 2) return 'warning' as const;
  return 'default' as const;
}

function statusBadgeVariant(statusCode: number | null | undefined) {
  if (statusCode == null) return 'default' as const;
  if (statusCode >= 200 && statusCode < 300) return 'success' as const;
  if (statusCode >= 400 && statusCode < 500) return 'warning' as const;
  if (statusCode >= 500) return 'danger' as const;
  return 'default' as const;
}

function actionBadgeVariant(action: number | null | undefined) {
  if (action == null) return 'default' as const;
  if (action === 0) return 'success' as const;
  if (action === 2) return 'danger' as const;
  if (action === 5 || action === 7) return 'warning' as const;
  return 'info' as const;
}

function formatTimestamp(iso: string): string {
  const d = new Date(iso);
  return d.toLocaleString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  });
}

function relativeTime(iso: string): string {
  const now = Date.now();
  const then = new Date(iso).getTime();
  const diffSec = Math.floor((now - then) / 1000);
  if (diffSec < 60) return 'just now';
  const diffMin = Math.floor(diffSec / 60);
  if (diffMin < 60) return `${diffMin}m ago`;
  const diffHr = Math.floor(diffMin / 60);
  if (diffHr < 24) return `${diffHr}h ago`;
  const diffDay = Math.floor(diffHr / 24);
  if (diffDay < 30) return `${diffDay}d ago`;
  return formatTimestamp(iso);
}

function buildFilterParams(f: Partial<AuditQueryRequest>, page?: number): Record<string, string> {
  const params: Record<string, string> = {};
  if (f.from) params.from = String(f.from);
  if (f.to) params.to = String(f.to);
  if (f.source != null) params.source = String(f.source);
  if (f.action != null) params.action = String(f.action);
  if (f.module) params.module = f.module;
  if (f.searchText) params.searchText = f.searchText;
  if (f.statusCode != null) params.statusCode = String(f.statusCode);
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

export default function Browse({ result, filters }: Props) {
  const [from, setFrom] = useState(filters.from ? String(filters.from) : '');
  const [to, setTo] = useState(filters.to ? String(filters.to) : '');
  const [source, setSource] = useState(filters.source != null ? String(filters.source) : '__all__');
  const [action, setAction] = useState(filters.action != null ? String(filters.action) : '__all__');
  const [module, setModule] = useState(filters.module ?? '');
  const [searchText, setSearchText] = useState(filters.searchText ?? '');
  const [statusCode, setStatusCode] = useState(
    filters.statusCode != null ? String(filters.statusCode) : '__all__',
  );

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
      statusCode: statusCode !== '__all__' ? Number(statusCode) : undefined,
    };
  }

  function applyFilters(e?: FormEvent) {
    e?.preventDefault();
    router.get('/audit-logs/browse', buildFilterParams(currentFilters()));
  }

  function clearFilters() {
    setFrom('');
    setTo('');
    setSource('__all__');
    setAction('__all__');
    setModule('');
    setSearchText('');
    setStatusCode('__all__');
    router.get('/audit-logs/browse');
  }

  function applyDatePreset(hours: number) {
    const now = new Date();
    const past = new Date(now.getTime() - hours * 60 * 60 * 1000);
    const toLocal = now.toISOString().slice(0, 16);
    const fromLocal = past.toISOString().slice(0, 16);
    setFrom(fromLocal);
    setTo(toLocal);
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

  const hasActiveFilters =
    from ||
    to ||
    source !== '__all__' ||
    action !== '__all__' ||
    module ||
    searchText ||
    statusCode !== '__all__';

  const startItem = (currentPage - 1) * result.pageSize + 1;
  const endItem = Math.min(currentPage * result.pageSize, result.totalCount);

  return (
    <TooltipProvider>
      <PageShell
        className="space-y-4"
        title="Audit Logs"
        description={`${result.totalCount.toLocaleString()} total entries`}
        actions={
          <div className="flex gap-2">
            <Button variant="secondary" onClick={() => exportLogs('csv')}>
              Export CSV
            </Button>
            <Button variant="secondary" onClick={() => exportLogs('json')}>
              Export JSON
            </Button>
          </div>
        }
      >
        {/* Filter Panel */}
        <Card>
          <CardContent>
            {/* Quick date presets */}
            <div className="mb-3 flex flex-wrap items-center gap-2">
              <span className="text-xs font-medium text-text-muted">Quick range:</span>
              {DATE_PRESETS.map((preset) => (
                <Button
                  key={preset.hours}
                  variant="ghost"
                  size="sm"
                  onClick={() => applyDatePreset(preset.hours)}
                >
                  {preset.label}
                </Button>
              ))}
            </div>

            <form onSubmit={applyFilters}>
              <div className="grid grid-cols-2 gap-4 md:grid-cols-3 lg:grid-cols-4">
                <div className="space-y-1">
                  <label htmlFor="filter-from" className="text-xs font-medium text-text-muted">
                    From
                  </label>
                  <Input
                    id="filter-from"
                    type="datetime-local"
                    value={from}
                    onChange={(e) => setFrom(e.target.value)}
                  />
                </div>
                <div className="space-y-1">
                  <label htmlFor="filter-to" className="text-xs font-medium text-text-muted">
                    To
                  </label>
                  <Input
                    id="filter-to"
                    type="datetime-local"
                    value={to}
                    onChange={(e) => setTo(e.target.value)}
                  />
                </div>
                <div className="space-y-1">
                  <span className="text-xs font-medium text-text-muted">Source</span>
                  <Select value={source} onValueChange={setSource}>
                    <SelectTrigger aria-label="Source">
                      <SelectValue placeholder="All sources" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="__all__">All sources</SelectItem>
                      {Object.entries(SOURCE_LABELS).map(([k, v]) => (
                        <SelectItem key={k} value={k}>
                          {v}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                <div className="space-y-1">
                  <span className="text-xs font-medium text-text-muted">Action</span>
                  <Select value={action} onValueChange={setAction}>
                    <SelectTrigger aria-label="Action">
                      <SelectValue placeholder="All actions" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="__all__">All actions</SelectItem>
                      {Object.entries(ACTION_LABELS).map(([k, v]) => (
                        <SelectItem key={k} value={k}>
                          {v}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                <div className="space-y-1">
                  <span className="text-xs font-medium text-text-muted">Status</span>
                  <Select value={statusCode} onValueChange={setStatusCode}>
                    <SelectTrigger aria-label="Status code">
                      <SelectValue placeholder="All statuses" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="__all__">All statuses</SelectItem>
                      <SelectItem value="200">2xx Success</SelectItem>
                      <SelectItem value="300">3xx Redirect</SelectItem>
                      <SelectItem value="400">4xx Client Error</SelectItem>
                      <SelectItem value="500">5xx Server Error</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
                <div className="space-y-1">
                  <label htmlFor="filter-module" className="text-xs font-medium text-text-muted">
                    Module
                  </label>
                  <Input
                    id="filter-module"
                    placeholder="e.g. Products"
                    value={module}
                    onChange={(e) => setModule(e.target.value)}
                  />
                </div>
                <div className="space-y-1">
                  <label htmlFor="filter-search" className="text-xs font-medium text-text-muted">
                    Search
                  </label>
                  <Input
                    id="filter-search"
                    placeholder="User, path, entity..."
                    value={searchText}
                    onChange={(e) => setSearchText(e.target.value)}
                  />
                </div>
                <div className="flex items-end gap-2">
                  <Button type="submit">Apply</Button>
                  {hasActiveFilters && (
                    <Button variant="ghost" onClick={clearFilters}>
                      Clear
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
              <p className="text-lg font-medium text-text">No audit logs found</p>
              <p className="mt-1 text-sm text-text-muted">
                {hasActiveFilters
                  ? 'Try adjusting your filters or selecting a different date range.'
                  : 'Audit entries will appear here as activity occurs.'}
              </p>
              {hasActiveFilters && (
                <Button variant="secondary" className="mt-4" onClick={clearFilters}>
                  Clear filters
                </Button>
              )}
            </CardContent>
          </Card>
        ) : (
          <Card>
            <CardContent className="p-0">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Time</TableHead>
                    <TableHead>Source</TableHead>
                    <TableHead>User</TableHead>
                    <TableHead>Action</TableHead>
                    <TableHead>Module</TableHead>
                    <TableHead>Path</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Duration</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {result.items.map((entry) => (
                    <TableRow
                      key={entry.id}
                      className="cursor-pointer"
                      onClick={() => router.get(`/audit-logs/${entry.id}`)}
                    >
                      <TableCell className="whitespace-nowrap text-sm">
                        <Tooltip>
                          <TooltipTrigger asChild>
                            <span className="text-text-muted">{relativeTime(entry.timestamp)}</span>
                          </TooltipTrigger>
                          <TooltipContent>{formatTimestamp(entry.timestamp)}</TooltipContent>
                        </Tooltip>
                      </TableCell>
                      <TableCell>
                        <Badge variant={sourceBadgeVariant(entry.source)}>
                          {SOURCE_LABELS[entry.source] ?? 'Unknown'}
                        </Badge>
                      </TableCell>
                      <TableCell className="text-sm">
                        {entry.userName || entry.userId || '\u2014'}
                      </TableCell>
                      <TableCell>
                        {entry.action != null ? (
                          <Badge variant={actionBadgeVariant(entry.action)}>
                            {ACTION_LABELS[entry.action] ?? 'Unknown'}
                          </Badge>
                        ) : (
                          '\u2014'
                        )}
                      </TableCell>
                      <TableCell className="text-sm">{entry.module || '\u2014'}</TableCell>
                      <TableCell className="max-w-[200px] text-sm text-text-muted">
                        {entry.path ? (
                          <Tooltip>
                            <TooltipTrigger asChild>
                              <span className="block truncate">{entry.path}</span>
                            </TooltipTrigger>
                            <TooltipContent>
                              <span className="font-mono text-xs">
                                {entry.httpMethod && `${entry.httpMethod} `}
                                {entry.path}
                              </span>
                            </TooltipContent>
                          </Tooltip>
                        ) : (
                          '\u2014'
                        )}
                      </TableCell>
                      <TableCell>
                        {entry.statusCode != null ? (
                          <Badge variant={statusBadgeVariant(entry.statusCode)}>
                            {entry.statusCode}
                          </Badge>
                        ) : (
                          '\u2014'
                        )}
                      </TableCell>
                      <TableCell className="text-sm text-text-muted">
                        {entry.durationMs != null ? `${entry.durationMs}ms` : '\u2014'}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </CardContent>
          </Card>
        )}

        {/* Server-side Pagination */}
        {result.totalCount > 0 && (
          <div className="flex items-center justify-between">
            <span className="text-sm text-text-muted">
              Showing {startItem}\u2013{endItem} of {result.totalCount.toLocaleString()} entries
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
                {paginationRange(currentPage, totalPages).map((p, i) =>
                  p === 'ellipsis' ? (
                    <span key={`ellipsis-${i}`} className="px-1 text-sm text-text-muted">
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
    </TooltipProvider>
  );
}

/** Build a compact pagination range: 1 ... 4 5 [6] 7 8 ... 20 */
function paginationRange(current: number, total: number): (number | 'ellipsis')[] {
  if (total <= 7) {
    return Array.from({ length: total }, (_, i) => i + 1);
  }
  const pages: (number | 'ellipsis')[] = [];
  pages.push(1);
  if (current > 3) pages.push('ellipsis');
  const start = Math.max(2, current - 1);
  const end = Math.min(total - 1, current + 1);
  for (let i = start; i <= end; i++) pages.push(i);
  if (current < total - 2) pages.push('ellipsis');
  pages.push(total);
  return pages;
}
