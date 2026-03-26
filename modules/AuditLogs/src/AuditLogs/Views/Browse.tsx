import { router } from '@inertiajs/react';
import {
  Badge,
  Button,
  Card,
  CardContent,
  DataGrid,
  Input,
  PageHeader,
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
import { useState } from 'react';
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

function buildFilterParams(f: Partial<AuditQueryRequest>): Record<string, string> {
  const params: Record<string, string> = {};
  if (f.from) params.from = String(f.from);
  if (f.to) params.to = String(f.to);
  if (f.source != null) params.source = String(f.source);
  if (f.action != null) params.action = String(f.action);
  if (f.module) params.module = f.module;
  if (f.searchText) params.searchText = f.searchText;
  return params;
}

export default function Browse({ result, filters }: Props) {
  const [from, setFrom] = useState(filters.from ? String(filters.from) : '');
  const [to, setTo] = useState(filters.to ? String(filters.to) : '');
  const [source, setSource] = useState(filters.source != null ? String(filters.source) : '__all__');
  const [action, setAction] = useState(filters.action != null ? String(filters.action) : '__all__');
  const [module, setModule] = useState(filters.module ?? '');
  const [searchText, setSearchText] = useState(filters.searchText ?? '');

  function applyFilters() {
    const params = buildFilterParams({
      from: from || undefined,
      to: to || undefined,
      source: source !== '__all__' ? Number(source) : undefined,
      action: action !== '__all__' ? Number(action) : undefined,
      module: module || undefined,
      searchText: searchText || undefined,
    });
    router.get('/audit-logs/browse', params);
  }

  function clearFilters() {
    setFrom('');
    setTo('');
    setSource('__all__');
    setAction('__all__');
    setModule('');
    setSearchText('');
    router.get('/audit-logs/browse');
  }

  function exportLogs(format: string) {
    const params = buildFilterParams({
      from: from || undefined,
      to: to || undefined,
      source: source !== '__all__' ? Number(source) : undefined,
      action: action !== '__all__' ? Number(action) : undefined,
      module: module || undefined,
      searchText: searchText || undefined,
    });
    const query = new URLSearchParams({ ...params, format }).toString();
    window.location.href = `/api/audit-logs/export?${query}`;
  }

  return (
    <div className="mx-auto max-w-7xl space-y-6">
      <PageHeader
        className="mb-0"
        title="Audit Logs"
        description={`${result.totalCount} total entries`}
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
      />

      <Card>
        <CardContent>
          <div className="grid grid-cols-2 gap-4 md:grid-cols-3 lg:grid-cols-6">
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
                  <SelectItem value="__all__">All</SelectItem>
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
                  <SelectItem value="__all__">All</SelectItem>
                  {Object.entries(ACTION_LABELS).map(([k, v]) => (
                    <SelectItem key={k} value={k}>
                      {v}
                    </SelectItem>
                  ))}
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
                placeholder="Search..."
                value={searchText}
                onChange={(e) => setSearchText(e.target.value)}
              />
            </div>
          </div>
          <div className="mt-4 flex gap-2">
            <Button onClick={applyFilters}>Apply</Button>
            <Button variant="secondary" onClick={clearFilters}>
              Clear
            </Button>
          </div>
        </CardContent>
      </Card>

      {result.items.length === 0 ? (
        <Card>
          <CardContent>
            <div className="flex flex-col items-center justify-center py-12 text-center">
              <svg
                className="mb-4 h-12 w-12 text-text-muted/50"
                fill="none"
                stroke="currentColor"
                strokeWidth="1.5"
                viewBox="0 0 24 24"
                role="img"
                aria-label="No audit logs"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m5.231 13.481L15 17.25m-4.5-15H5.625c-.621 0-1.125.504-1.125 1.125v16.5c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z"
                />
              </svg>
              <h3 className="text-sm font-medium">No audit logs found</h3>
              <p className="mt-1 text-sm text-text-muted">
                Try adjusting your filters or check back later.
              </p>
            </div>
          </CardContent>
        </Card>
      ) : (
        <DataGrid data={result.items}>
          {(pageData) => (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Timestamp</TableHead>
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
                {pageData.map((entry) => (
                  <TableRow
                    key={entry.id}
                    className="cursor-pointer"
                    onClick={() => router.get(`/audit-logs/${entry.id}`)}
                  >
                    <TableCell className="whitespace-nowrap text-sm text-text-muted">
                      {formatTimestamp(entry.timestamp)}
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
                    <TableCell className="max-w-[200px] truncate text-sm text-text-muted">
                      {entry.path || '\u2014'}
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
          )}
        </DataGrid>
      )}
    </div>
  );
}
