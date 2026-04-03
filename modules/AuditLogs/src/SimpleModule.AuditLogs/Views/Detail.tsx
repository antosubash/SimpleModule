import { router } from '@inertiajs/react';
import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  PageShell,
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
import { useEffect, useState } from 'react';
import { AuditLogsKeys } from '../Locales/keys';
import type { AuditEntry } from '../types';
import {
  ACTION_LABELS,
  actionBadgeVariant,
  formatTimestamp,
  relativeTime,
  SOURCE_LABELS,
  sourceBadgeVariant,
  statusBadgeVariant,
} from '../utils/audit-utils';

interface Props {
  entry: AuditEntry;
  correlated: AuditEntry[];
}

function LabeledField({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <dt className="text-sm text-text-muted">{label}</dt>
      <dd className="mt-1 text-sm font-medium text-text">{children || '\u2014'}</dd>
    </div>
  );
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

function CopyButton({
  text,
  labelCopy,
  labelCopied,
}: {
  text: string;
  labelCopy: string;
  labelCopied: string;
}) {
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    if (!copied) return;
    const id = setTimeout(() => setCopied(false), 2000);
    return () => clearTimeout(id);
  }, [copied]);

  function handleCopy() {
    navigator.clipboard.writeText(text).catch(() => {});
    setCopied(true);
  }

  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <button
          type="button"
          onClick={handleCopy}
          className="ml-2 inline-flex items-center rounded p-1 text-text-muted hover:bg-surface-secondary hover:text-text"
        >
          {copied ? (
            <svg
              className="h-3.5 w-3.5"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              viewBox="0 0 24 24"
              aria-hidden="true"
            >
              <path d="M20 6 9 17l-5-5" />
            </svg>
          ) : (
            <svg
              className="h-3.5 w-3.5"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              viewBox="0 0 24 24"
              aria-hidden="true"
            >
              <rect x="9" y="9" width="13" height="13" rx="2" />
              <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1" />
            </svg>
          )}
        </button>
      </TooltipTrigger>
      <TooltipContent>{copied ? labelCopied : labelCopy}</TooltipContent>
    </Tooltip>
  );
}

export default function Detail({ entry, correlated }: Props) {
  const { t } = useTranslation('AuditLogs');
  const showHttp = !!entry.httpMethod;
  const showDomain = !!(entry.module || entry.entityType || entry.action != null);
  const showChanges = !!entry.changes;
  const showMetadata = !!entry.metadata;

  const changes = showChanges ? parseChanges(entry.changes) : [];
  const isUpdate = hasUpdateStyle(changes);

  return (
    <TooltipProvider>
      <PageShell
        title={t(AuditLogsKeys.Detail.Title, { id: entry.id })}
        actions={
          <Button variant="secondary" onClick={() => router.get('/audit-logs/browse')}>
            {t(AuditLogsKeys.Detail.BackToBrowse)}
          </Button>
        }
        breadcrumbs={[
          { label: t(AuditLogsKeys.Detail.BreadcrumbAuditLogs), href: '/audit-logs/browse' },
          { label: t(AuditLogsKeys.Detail.BreadcrumbEntry, { id: entry.id }) },
        ]}
      >
        {/* Overview Card */}
        <Card>
          <CardHeader>
            <CardTitle>{t(AuditLogsKeys.Detail.OverviewTitle)}</CardTitle>
          </CardHeader>
          <CardContent>
            <dl className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
              <LabeledField label={t(AuditLogsKeys.Detail.FieldTimestamp)}>
                <Tooltip>
                  <TooltipTrigger asChild>
                    <span>{relativeTime(entry.timestamp)}</span>
                  </TooltipTrigger>
                  <TooltipContent>{formatTimestamp(entry.timestamp)}</TooltipContent>
                </Tooltip>
              </LabeledField>
              <LabeledField label={t(AuditLogsKeys.Detail.FieldSource)}>
                <Badge variant={sourceBadgeVariant(entry.source)}>
                  {SOURCE_LABELS[entry.source] ?? `Unknown (${entry.source})`}
                </Badge>
              </LabeledField>
              <LabeledField label={t(AuditLogsKeys.Detail.FieldCorrelationId)}>
                <span className="inline-flex items-center">
                  <span className="font-mono text-xs">{entry.correlationId}</span>
                  <CopyButton
                    text={entry.correlationId}
                    labelCopy={t(AuditLogsKeys.Detail.CopyToClipboard)}
                    labelCopied={t(AuditLogsKeys.Detail.CopyCopied)}
                  />
                </span>
              </LabeledField>
              <LabeledField label={t(AuditLogsKeys.Detail.FieldUser)}>
                {entry.userName || entry.userId}
              </LabeledField>
              <LabeledField label={t(AuditLogsKeys.Detail.FieldIpAddress)}>
                {entry.ipAddress}
              </LabeledField>
              <LabeledField label={t(AuditLogsKeys.Detail.FieldUserAgent)}>
                {entry.userAgent}
              </LabeledField>
            </dl>
          </CardContent>
        </Card>

        {/* HTTP Details Card */}
        {showHttp && (
          <Card>
            <CardHeader>
              <CardTitle>{t(AuditLogsKeys.Detail.HttpDetailsTitle)}</CardTitle>
            </CardHeader>
            <CardContent>
              <dl className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
                <LabeledField label={t(AuditLogsKeys.Detail.FieldMethodPath)}>
                  <Badge variant="info" className="mr-2">
                    {entry.httpMethod}
                  </Badge>
                  <span className="font-mono text-xs">{entry.path}</span>
                </LabeledField>
                <LabeledField label={t(AuditLogsKeys.Detail.FieldQueryString)}>
                  {entry.queryString ? (
                    <span className="font-mono text-xs">{entry.queryString}</span>
                  ) : (
                    '\u2014'
                  )}
                </LabeledField>
                <LabeledField label={t(AuditLogsKeys.Detail.FieldStatusCode)}>
                  <Badge variant={statusBadgeVariant(entry.statusCode)}>{entry.statusCode}</Badge>
                </LabeledField>
                <LabeledField label={t(AuditLogsKeys.Detail.FieldDuration)}>
                  {entry.durationMs != null ? `${entry.durationMs}ms` : '\u2014'}
                </LabeledField>
              </dl>
              {entry.requestBody && (
                <div className="mt-4">
                  <p className="mb-1 text-sm text-text-muted">
                    {t(AuditLogsKeys.Detail.RequestBody)}
                  </p>
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
              <CardTitle>{t(AuditLogsKeys.Detail.DomainDetailsTitle)}</CardTitle>
            </CardHeader>
            <CardContent>
              <dl className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
                <LabeledField label={t(AuditLogsKeys.Detail.FieldModule)}>
                  {entry.module}
                </LabeledField>
                <LabeledField label={t(AuditLogsKeys.Detail.FieldEntityType)}>
                  {entry.entityType}
                </LabeledField>
                <LabeledField label={t(AuditLogsKeys.Detail.FieldEntityId)}>
                  {entry.entityId}
                </LabeledField>
                <LabeledField label={t(AuditLogsKeys.Detail.FieldAction)}>
                  {entry.action != null ? (
                    <Badge variant={actionBadgeVariant(entry.action)}>
                      {ACTION_LABELS[entry.action] ?? `Unknown (${entry.action})`}
                    </Badge>
                  ) : (
                    '\u2014'
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
              <CardTitle>{t(AuditLogsKeys.Detail.ChangesTitle)}</CardTitle>
            </CardHeader>
            <CardContent>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>{t(AuditLogsKeys.Detail.ColField)}</TableHead>
                    {isUpdate ? (
                      <>
                        <TableHead>{t(AuditLogsKeys.Detail.ColOldValue)}</TableHead>
                        <TableHead>{t(AuditLogsKeys.Detail.ColNewValue)}</TableHead>
                      </>
                    ) : (
                      <TableHead>{t(AuditLogsKeys.Detail.ColValue)}</TableHead>
                    )}
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {changes.map((change) => (
                    <TableRow key={change.field}>
                      <TableCell className="font-medium">{change.field}</TableCell>
                      {isUpdate ? (
                        <>
                          <TableCell>
                            {change.old != null ? (
                              <span className="inline-block rounded bg-danger/10 px-1.5 py-0.5 font-mono text-xs text-danger">
                                {String(change.old)}
                              </span>
                            ) : (
                              <span className="text-text-muted">\u2014</span>
                            )}
                          </TableCell>
                          <TableCell>
                            {change.new != null ? (
                              <span className="inline-block rounded bg-success/10 px-1.5 py-0.5 font-mono text-xs text-success">
                                {String(change.new)}
                              </span>
                            ) : (
                              <span className="text-text-muted">\u2014</span>
                            )}
                          </TableCell>
                        </>
                      ) : (
                        <TableCell className="font-mono text-xs">
                          {change.value != null ? String(change.value) : '\u2014'}
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
              <CardTitle>{t(AuditLogsKeys.Detail.MetadataTitle)}</CardTitle>
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
              <CardTitle>
                {t(AuditLogsKeys.Detail.CorrelatedTitle)}
                <span className="ml-2 text-sm font-normal text-text-muted">
                  {t(AuditLogsKeys.Detail.CorrelatedRelated, { count: correlated.length })}
                </span>
              </CardTitle>
            </CardHeader>
            <CardContent>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>{t(AuditLogsKeys.Detail.ColId)}</TableHead>
                    <TableHead>{t(AuditLogsKeys.Detail.ColTime)}</TableHead>
                    <TableHead>{t(AuditLogsKeys.Detail.ColSource)}</TableHead>
                    <TableHead>{t(AuditLogsKeys.Detail.ColAction)}</TableHead>
                    <TableHead>{t(AuditLogsKeys.Detail.ColModule)}</TableHead>
                    <TableHead>{t(AuditLogsKeys.Detail.ColPath)}</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {correlated.map((e) => (
                    <TableRow
                      key={e.id}
                      className={`cursor-pointer ${e.id === entry.id ? 'bg-primary/5' : ''}`}
                      onClick={() => router.get(`/audit-logs/${e.id}`)}
                    >
                      <TableCell className="text-text-muted">#{e.id}</TableCell>
                      <TableCell className="text-sm">
                        <Tooltip>
                          <TooltipTrigger asChild>
                            <span>{relativeTime(e.timestamp)}</span>
                          </TooltipTrigger>
                          <TooltipContent>{formatTimestamp(e.timestamp)}</TooltipContent>
                        </Tooltip>
                      </TableCell>
                      <TableCell>
                        <Badge variant={sourceBadgeVariant(e.source)}>
                          {SOURCE_LABELS[e.source] ?? 'Unknown'}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        {e.action != null ? (
                          <Badge variant={actionBadgeVariant(e.action)}>
                            {ACTION_LABELS[e.action] ?? `Unknown (${e.action})`}
                          </Badge>
                        ) : (
                          '\u2014'
                        )}
                      </TableCell>
                      <TableCell className="text-sm">{e.module || '\u2014'}</TableCell>
                      <TableCell className="font-mono text-xs">{e.path || '\u2014'}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </CardContent>
          </Card>
        )}
      </PageShell>
    </TooltipProvider>
  );
}
