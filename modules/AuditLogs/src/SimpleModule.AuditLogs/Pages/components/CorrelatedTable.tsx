import { router } from '@inertiajs/react';
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
} from '@/utils/audit-utils';

interface Props {
  correlated: AuditEntry[];
  currentEntryId: number;
}

export function CorrelatedTable({ correlated, currentEntryId }: Props) {
  const { t } = useTranslation('AuditLogs');
  return (
    <Card>
      <CardHeader>
        <CardTitle>
          {t(AuditLogsKeys.Detail.CorrelatedTitle)}
          <span className="ml-2 text-sm font-normal text-text-muted">
            {t(AuditLogsKeys.Detail.CorrelatedRelated, { count: String(correlated.length) })}
          </span>
        </CardTitle>
      </CardHeader>
      <CardContent className="p-0">
        <div className="overflow-x-auto -mx-4 px-4 sm:mx-0 sm:px-0">
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
                  className={`cursor-pointer ${e.id === currentEntryId ? 'bg-primary/5' : ''}`}
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
        </div>
      </CardContent>
    </Card>
  );
}
