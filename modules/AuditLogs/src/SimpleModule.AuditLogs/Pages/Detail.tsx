import { router } from '@inertiajs/react';
import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  PageShell,
  TooltipProvider,
} from '@simplemodule/ui';
import { AuditLogsKeys } from '@/Locales/keys';
import type { AuditEntry } from '@/types';
import { CorrelatedTable } from './components/CorrelatedTable';
import {
  ChangesCard,
  DomainDetailsCard,
  HttpDetailsCard,
  OverviewCard,
} from './components/DetailCards';
import { formatJson, parseChanges } from './components/DetailShared';

interface Props {
  entry: AuditEntry;
  correlated: AuditEntry[];
}

export default function Detail({ entry, correlated }: Props) {
  const { t } = useTranslation('AuditLogs');
  const showHttp = !!entry.httpMethod;
  const showDomain = !!(entry.module || entry.entityType || entry.action != null);
  const showChanges = !!entry.changes;
  const showMetadata = !!entry.metadata;

  const changes = showChanges ? parseChanges(entry.changes) : [];

  return (
    <TooltipProvider>
      <PageShell
        className="space-y-4 sm:space-y-6"
        title={t(AuditLogsKeys.Detail.Title, { id: String(entry.id) })}
        actions={
          <Button variant="secondary" onClick={() => router.get('/audit-logs/browse')}>
            {t(AuditLogsKeys.Detail.BackToBrowse)}
          </Button>
        }
        breadcrumbs={[
          { label: t(AuditLogsKeys.Detail.BreadcrumbAuditLogs), href: '/audit-logs/browse' },
          { label: t(AuditLogsKeys.Detail.BreadcrumbEntry, { id: String(entry.id) }) },
        ]}
      >
        <OverviewCard entry={entry} />

        {showHttp && <HttpDetailsCard entry={entry} />}

        {showDomain && <DomainDetailsCard entry={entry} />}

        {showChanges && changes.length > 0 && <ChangesCard changes={changes} />}

        {showMetadata && (
          <Card>
            <CardHeader>
              <CardTitle>{t(AuditLogsKeys.Detail.MetadataTitle)}</CardTitle>
            </CardHeader>
            <CardContent className="p-4 sm:p-6">
              <pre className="overflow-auto rounded-md bg-surface-secondary p-2 sm:p-3 text-xs">
                {formatJson(entry.metadata)}
              </pre>
            </CardContent>
          </Card>
        )}

        {correlated.length > 0 && (
          <CorrelatedTable correlated={correlated} currentEntryId={entry.id} />
        )}
      </PageShell>
    </TooltipProvider>
  );
}
