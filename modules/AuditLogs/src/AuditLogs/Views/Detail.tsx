import { router } from '@inertiajs/react';
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Container,
  PageHeader,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';
import type { AuditEntry } from '../types';

interface Props {
  entry: AuditEntry;
  correlated: AuditEntry[];
}

const SOURCE_LABELS: Record<number, string> = { 0: 'HTTP', 1: 'Domain', 2: 'Changes' };
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

function LabeledField({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <dt className="text-sm text-text-muted">{label}</dt>
      <dd className="mt-1 text-sm font-medium text-text">{children || '—'}</dd>
    </div>
  );
}

function formatTimestamp(ts: string): string {
  return new Date(ts).toLocaleString();
}

function formatJson(raw: string): string {
  try {
    return JSON.stringify(JSON.parse(raw), null, 2);
  } catch {
    return raw;
  }
}

interface ChangeEntry {
  field: string;
  old?: unknown;
  new?: unknown;
  value?: unknown;
}

function parseChanges(raw: string): ChangeEntry[] {
  try {
    const parsed = JSON.parse(raw);
    if (Array.isArray(parsed)) {
      return (parsed as Array<Record<string, unknown>>).map((item) => {
        // Normalize both { old, new } and { oldValue, newValue } formats
        if ('old' in item || 'new' in item) {
          return { field: String(item.field ?? ''), old: item.old, new: item.new };
        }
        if ('oldValue' in item || 'newValue' in item) {
          return { field: String(item.field ?? ''), old: item.oldValue, new: item.newValue };
        }
        return { field: String(item.field ?? ''), value: item.value };
      });
    }
    if (typeof parsed === 'object' && parsed !== null) {
      return Object.entries(parsed).map(([field, val]) => {
        if (
          typeof val === 'object' &&
          val !== null &&
          ('old' in val || 'new' in val || 'oldValue' in val || 'newValue' in val)
        ) {
          const v = val as { old?: unknown; new?: unknown; oldValue?: unknown; newValue?: unknown };
          return { field, old: v.old ?? v.oldValue, new: v.new ?? v.newValue };
        }
        return { field, value: val };
      });
    }
    return [];
  } catch {
    return [];
  }
}

function hasUpdateStyle(changes: ChangeEntry[]): boolean {
  return changes.some((c) => 'old' in c || 'new' in c);
}

export default function Detail({ entry, correlated }: Props) {
  const showHttp = !!entry.httpMethod;
  const showDomain = !!(entry.module || entry.entityType || entry.action != null);
  const showChanges = !!entry.changes;
  const showMetadata = !!entry.metadata;

  const changes = showChanges ? parseChanges(entry.changes) : [];
  const isUpdate = hasUpdateStyle(changes);

  return (
    <Container className="space-y-6">
      <PageHeader
        className="mb-0"
        title={`Audit Entry #${entry.id}`}
        actions={
          <Button variant="secondary" onClick={() => router.get('/audit-logs/browse')}>
            Back to Browse
          </Button>
        }
      />

      {/* Overview Card */}
      <Card>
        <CardHeader>
          <CardTitle>Overview</CardTitle>
        </CardHeader>
        <CardContent>
          <dl className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
            <LabeledField label="Timestamp">{formatTimestamp(entry.timestamp)}</LabeledField>
            <LabeledField label="Source">
              {SOURCE_LABELS[entry.source] ?? `Unknown (${entry.source})`}
            </LabeledField>
            <LabeledField label="Correlation ID">
              <span className="font-mono text-xs">{entry.correlationId}</span>
            </LabeledField>
            <LabeledField label="User">{entry.userName || entry.userId}</LabeledField>
            <LabeledField label="IP Address">{entry.ipAddress}</LabeledField>
            <LabeledField label="User Agent">{entry.userAgent}</LabeledField>
          </dl>
        </CardContent>
      </Card>

      {/* HTTP Details Card */}
      {showHttp && (
        <Card>
          <CardHeader>
            <CardTitle>HTTP Details</CardTitle>
          </CardHeader>
          <CardContent>
            <dl className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
              <LabeledField label="Method + Path">
                <Badge variant="outline" className="mr-2">
                  {entry.httpMethod}
                </Badge>
                <span className="font-mono text-xs">{entry.path}</span>
              </LabeledField>
              <LabeledField label="Query String">
                {entry.queryString ? (
                  <span className="font-mono text-xs">{entry.queryString}</span>
                ) : (
                  '—'
                )}
              </LabeledField>
              <LabeledField label="Status Code">
                <Badge
                  variant={
                    entry.statusCode >= 200 && entry.statusCode < 300 ? 'default' : 'destructive'
                  }
                >
                  {entry.statusCode}
                </Badge>
              </LabeledField>
              <LabeledField label="Duration">
                {entry.durationMs != null ? `${entry.durationMs}ms` : '—'}
              </LabeledField>
            </dl>
            {entry.requestBody && (
              <div className="mt-4">
                <p className="mb-1 text-sm text-text-muted">Request Body</p>
                <pre className="overflow-auto rounded-md bg-surface-secondary p-3 text-xs">
                  {formatJson(entry.requestBody)}
                </pre>
              </div>
            )}
          </CardContent>
        </Card>
      )}

      {/* Domain Details Card */}
      {showDomain && (
        <Card>
          <CardHeader>
            <CardTitle>Domain Details</CardTitle>
          </CardHeader>
          <CardContent>
            <dl className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
              <LabeledField label="Module">{entry.module}</LabeledField>
              <LabeledField label="Entity Type">{entry.entityType}</LabeledField>
              <LabeledField label="Entity ID">{entry.entityId}</LabeledField>
              <LabeledField label="Action">
                {entry.action != null ? (
                  <Badge>{ACTION_LABELS[entry.action] ?? `Unknown (${entry.action})`}</Badge>
                ) : (
                  '—'
                )}
              </LabeledField>
            </dl>
          </CardContent>
        </Card>
      )}

      {/* Changes Card */}
      {showChanges && changes.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Changes</CardTitle>
          </CardHeader>
          <CardContent>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Field</TableHead>
                  {isUpdate ? (
                    <>
                      <TableHead>Old</TableHead>
                      <TableHead>New</TableHead>
                    </>
                  ) : (
                    <TableHead>Value</TableHead>
                  )}
                </TableRow>
              </TableHeader>
              <TableBody>
                {changes.map((change) => (
                  <TableRow key={change.field}>
                    <TableCell className="font-medium">{change.field}</TableCell>
                    {isUpdate ? (
                      <>
                        <TableCell className="font-mono text-xs text-text-muted">
                          {change.old != null ? String(change.old) : '—'}
                        </TableCell>
                        <TableCell className="font-mono text-xs">
                          {change.new != null ? String(change.new) : '—'}
                        </TableCell>
                      </>
                    ) : (
                      <TableCell className="font-mono text-xs">
                        {change.value != null ? String(change.value) : '—'}
                      </TableCell>
                    )}
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      )}

      {/* Metadata Card */}
      {showMetadata && (
        <Card>
          <CardHeader>
            <CardTitle>Metadata</CardTitle>
          </CardHeader>
          <CardContent>
            <pre className="overflow-auto rounded-md bg-surface-secondary p-3 text-xs">
              {formatJson(entry.metadata)}
            </pre>
          </CardContent>
        </Card>
      )}

      {/* Correlated Entries Card */}
      {correlated.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Correlated Entries</CardTitle>
          </CardHeader>
          <CardContent>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>ID</TableHead>
                  <TableHead>Timestamp</TableHead>
                  <TableHead>Source</TableHead>
                  <TableHead>Action</TableHead>
                  <TableHead>Path</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {correlated.map((e) => (
                  <TableRow
                    key={e.id}
                    className="cursor-pointer"
                    onClick={() => router.get(`/audit-logs/${e.id}`)}
                  >
                    <TableCell className="text-text-muted">#{e.id}</TableCell>
                    <TableCell>{formatTimestamp(e.timestamp)}</TableCell>
                    <TableCell>{SOURCE_LABELS[e.source] ?? `Unknown`}</TableCell>
                    <TableCell>
                      {e.action != null ? (
                        <Badge variant="outline">
                          {ACTION_LABELS[e.action] ?? `Unknown (${e.action})`}
                        </Badge>
                      ) : (
                        '—'
                      )}
                    </TableCell>
                    <TableCell className="font-mono text-xs">{e.path || '—'}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      )}
    </Container>
  );
}
