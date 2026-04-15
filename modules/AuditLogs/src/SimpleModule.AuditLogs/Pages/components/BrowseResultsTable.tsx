import { router } from '@inertiajs/react';
import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Badge,
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

export function BrowseResultsTable({ items }: { items: AuditEntry[] }) {
  const { t } = useTranslation('AuditLogs');

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>{t(AuditLogsKeys.Browse.ColTime)}</TableHead>
          <TableHead>{t(AuditLogsKeys.Browse.ColSource)}</TableHead>
          <TableHead>{t(AuditLogsKeys.Browse.ColUser)}</TableHead>
          <TableHead>{t(AuditLogsKeys.Browse.ColAction)}</TableHead>
          <TableHead>{t(AuditLogsKeys.Browse.ColModule)}</TableHead>
          <TableHead>{t(AuditLogsKeys.Browse.ColPath)}</TableHead>
          <TableHead>{t(AuditLogsKeys.Browse.ColStatus)}</TableHead>
          <TableHead>{t(AuditLogsKeys.Browse.ColDuration)}</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {items.map((entry) => (
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
            <TableCell className="text-sm">{entry.userName || entry.userId || '\u2014'}</TableCell>
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
                <Badge variant={statusBadgeVariant(entry.statusCode)}>{entry.statusCode}</Badge>
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
  );
}
