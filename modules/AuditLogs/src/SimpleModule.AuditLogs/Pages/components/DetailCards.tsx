import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Badge,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@simplemodule/ui';
import { AuditLogsKeys } from '@/Locales/keys';
import type { AuditEntry } from '@/types';
import {
  ACTION_LABELS,
  actionBadgeVariant,
  formatTimestamp,
  relativeTime,
  SOURCE_LABELS,
  sourceBadgeVariant,
  statusBadgeVariant,
} from '@/utils/audit-utils';
import {
  type ChangeEntry,
  CopyButton,
  formatJson,
  hasUpdateStyle,
  LabeledField,
} from './DetailShared';

export function OverviewCard({ entry }: { entry: AuditEntry }) {
  const { t } = useTranslation('AuditLogs');
  return (
    <Card>
      <CardHeader>
        <CardTitle>{t(AuditLogsKeys.Detail.OverviewTitle)}</CardTitle>
      </CardHeader>
      <CardContent>
        <dl className="grid grid-cols-1 gap-3 sm:grid-cols-2 sm:gap-4 lg:grid-cols-3">
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
              <CopyButton text={entry.correlationId} />
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
  );
}

export function HttpDetailsCard({ entry }: { entry: AuditEntry }) {
  const { t } = useTranslation('AuditLogs');
  return (
    <Card>
      <CardHeader>
        <CardTitle>{t(AuditLogsKeys.Detail.HttpDetailsTitle)}</CardTitle>
      </CardHeader>
      <CardContent>
        <dl className="grid grid-cols-1 gap-3 sm:grid-cols-2 sm:gap-4 lg:grid-cols-3">
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
            <p className="mb-1 text-sm text-text-muted">{t(AuditLogsKeys.Detail.RequestBody)}</p>
            <pre className="overflow-auto rounded-md bg-surface-secondary p-2 sm:p-3 text-xs">
              {formatJson(entry.requestBody)}
            </pre>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

export function DomainDetailsCard({ entry }: { entry: AuditEntry }) {
  const { t } = useTranslation('AuditLogs');
  return (
    <Card>
      <CardHeader>
        <CardTitle>{t(AuditLogsKeys.Detail.DomainDetailsTitle)}</CardTitle>
      </CardHeader>
      <CardContent>
        <dl className="grid grid-cols-1 gap-3 sm:grid-cols-2 sm:gap-4 lg:grid-cols-3">
          <LabeledField label={t(AuditLogsKeys.Detail.FieldModule)}>{entry.module}</LabeledField>
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
  );
}

export function ChangesCard({ changes }: { changes: ChangeEntry[] }) {
  const { t } = useTranslation('AuditLogs');
  const isUpdate = hasUpdateStyle(changes);
  return (
    <Card>
      <CardHeader>
        <CardTitle>{t(AuditLogsKeys.Detail.ChangesTitle)}</CardTitle>
      </CardHeader>
      <CardContent className="p-4 sm:p-6">
        <div className="overflow-x-auto -mx-4 px-4 sm:mx-0 sm:px-0">
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
        </div>
      </CardContent>
    </Card>
  );
}
