import { router } from '@inertiajs/react';
import { useTranslation } from '@simplemodule/client/use-translation';
import { type ChartConfig, PageShell } from '@simplemodule/ui';
import { useState } from 'react';
import { AuditLogsKeys } from '@/Locales/keys';
import type { DashboardStats, NamedCount } from '@/types';
import { DonutCard, HBarCard } from './components/DashboardCharts';
import { DashboardFilters } from './components/DashboardFilters';
import {
  dictToChartData,
  formatDate,
  PALETTE,
  SOURCE_COLORS,
  STATUS_COLORS,
} from './components/dashboard-constants';
import {
  EntityTypesChart,
  HourlyDistributionChart,
  TimelineAreaChart,
} from './components/InlineCharts';
import { KpiCard } from './components/KpiCard';

interface Props {
  stats: DashboardStats;
  from: string;
  to: string;
  userId: string;
  users: NamedCount[];
}

export default function Dashboard({ stats, from, to, userId, users }: Props) {
  const { t } = useTranslation('AuditLogs');
  const [dateFrom, setDateFrom] = useState<Date | undefined>(new Date(from));
  const [dateTo, setDateTo] = useState<Date | undefined>(new Date(to));
  const [selectedUser, setSelectedUser] = useState(userId || '__all__');

  function applyFilters() {
    const params: Record<string, string> = {};
    if (dateFrom) params.from = dateFrom.toISOString();
    if (dateTo) params.to = dateTo.toISOString();
    if (selectedUser && selectedUser !== '__all__') params.userId = selectedUser;
    router.get('/audit-logs/dashboard', params);
  }

  function applyDatePreset(hours: number) {
    const now = new Date();
    const past = new Date(now.getTime() - hours * 60 * 60 * 1000);
    const params: Record<string, string> = {
      from: past.toISOString(),
      to: now.toISOString(),
    };
    if (selectedUser && selectedUser !== '__all__') params.userId = selectedUser;
    router.get('/audit-logs/dashboard', params);
  }

  function browseWithFilter(params: Record<string, string>) {
    const toLocal = (iso: string) => iso.slice(0, 16);
    router.get('/audit-logs/browse', { from: toLocal(from), to: toLocal(to), ...params });
  }

  // Prepare chart data
  const sourceData = dictToChartData(stats.bySource);
  const actionData = dictToChartData(stats.byAction);
  const moduleData = dictToChartData(stats.byModule);
  const statusData = dictToChartData(stats.byStatusCategory);
  const entityData = dictToChartData(stats.byEntityType);

  // Chart configs
  const timelineConfig: ChartConfig = {
    http: { label: 'HTTP', color: SOURCE_COLORS.Http },
    domain: { label: 'Domain', color: SOURCE_COLORS.Domain },
    changes: { label: 'Changes', color: SOURCE_COLORS.ChangeTracker },
  };

  const sourceConfig: ChartConfig = Object.fromEntries(
    sourceData.map((d, i) => [
      d.name,
      { label: d.name, color: SOURCE_COLORS[d.name] || PALETTE[i % PALETTE.length] },
    ]),
  );

  const statusConfig: ChartConfig = Object.fromEntries(
    statusData.map((d, i) => [
      d.name,
      { label: d.name, color: STATUS_COLORS[d.name] || PALETTE[i % PALETTE.length] },
    ]),
  );

  const actionConfig: ChartConfig = Object.fromEntries(
    actionData.map((d, i) => [d.name, { label: d.name, color: PALETTE[i % PALETTE.length] }]),
  );

  const moduleConfig: ChartConfig = Object.fromEntries(
    moduleData.map((d, i) => [d.name, { label: d.name, color: PALETTE[i % PALETTE.length] }]),
  );

  const hourlyConfig: ChartConfig = {
    count: { label: 'Events', color: 'var(--color-primary)' },
  };

  const topUsersConfig: ChartConfig = {
    count: { label: 'Events', color: 'var(--color-info)' },
  };

  const topPathsConfig: ChartConfig = {
    count: { label: 'Hits', color: 'var(--color-primary)' },
  };

  const entityConfig: ChartConfig = Object.fromEntries(
    entityData.map((d, i) => [d.name, { label: d.name, color: PALETTE[i % PALETTE.length] }]),
  );

  return (
    <PageShell
      className="space-y-4 sm:space-y-6"
      title={t(AuditLogsKeys.Dashboard.Title)}
      description={t(AuditLogsKeys.Dashboard.Description)}
      actions={
        <DashboardFilters
          dateFrom={dateFrom}
          dateTo={dateTo}
          onDateFromChange={setDateFrom}
          onDateToChange={setDateTo}
          selectedUser={selectedUser}
          onSelectedUserChange={setSelectedUser}
          users={users}
          onApplyFilters={applyFilters}
          onApplyDatePreset={applyDatePreset}
        />
      }
    >
      {/* KPI Cards */}
      <div className="grid grid-cols-2 gap-3 sm:gap-4 md:grid-cols-4">
        <KpiCard
          title={t(AuditLogsKeys.Dashboard.KpiTotalEvents)}
          value={stats.totalEntries.toLocaleString()}
          subtitle={`${formatDate(from)} \u2014 ${formatDate(to)}`}
          onClick={() => browseWithFilter({})}
        />
        <KpiCard
          title={t(AuditLogsKeys.Dashboard.KpiUniqueUsers)}
          value={stats.uniqueUsers.toLocaleString()}
        />
        <KpiCard
          title={t(AuditLogsKeys.Dashboard.KpiAvgResponse)}
          value={stats.averageDurationMs > 0 ? `${stats.averageDurationMs}ms` : '\u2014'}
          subtitle={t(AuditLogsKeys.Dashboard.KpiHttpRequests)}
          onClick={() => browseWithFilter({ source: '0' })}
        />
        <KpiCard
          title={t(AuditLogsKeys.Dashboard.KpiErrorRate)}
          value={stats.errorRate > 0 ? `${stats.errorRate}%` : '0%'}
          accent={stats.errorRate > 5 ? 'danger' : 'default'}
          subtitle={t(AuditLogsKeys.Dashboard.KpiErrorRateSubtitle)}
          onClick={() => browseWithFilter({})}
        />
      </div>

      {/* Timeline Area Chart */}
      {stats.timeline.length > 0 && (
        <TimelineAreaChart
          title={t(AuditLogsKeys.Dashboard.ActivityTimeline)}
          data={stats.timeline}
          config={timelineConfig}
        />
      )}

      {/* Row: Source Pie + Action Bar */}
      <div className="grid grid-cols-1 gap-3 sm:gap-4 lg:grid-cols-2">
        {sourceData.length > 0 && (
          <DonutCard
            title={t(AuditLogsKeys.Dashboard.BySource)}
            data={sourceData}
            colors={SOURCE_COLORS}
            config={sourceConfig}
          />
        )}
        {actionData.length > 0 && (
          <HBarCard
            title={t(AuditLogsKeys.Dashboard.ByAction)}
            data={actionData}
            dataKey="value"
            config={actionConfig}
          />
        )}
      </div>

      {/* Row: Status Pie + Module Bar */}
      <div className="grid grid-cols-1 gap-3 sm:gap-4 lg:grid-cols-2">
        {statusData.length > 0 && (
          <DonutCard
            title={t(AuditLogsKeys.Dashboard.StatusCodes)}
            data={statusData}
            colors={STATUS_COLORS}
            config={statusConfig}
          />
        )}
        {moduleData.length > 0 && (
          <HBarCard
            title={t(AuditLogsKeys.Dashboard.ByModule)}
            data={moduleData}
            dataKey="value"
            config={moduleConfig}
          />
        )}
      </div>

      {/* Row: Top Users + Top Paths */}
      <div className="grid grid-cols-1 gap-3 sm:gap-4 lg:grid-cols-2">
        {stats.topUsers.length > 0 && (
          <HBarCard
            title={t(AuditLogsKeys.Dashboard.TopUsers)}
            data={stats.topUsers}
            dataKey="count"
            config={topUsersConfig}
            fill="var(--color-info)"
            yAxisWidth={120}
          />
        )}
        {stats.topPaths.length > 0 && (
          <HBarCard
            title={t(AuditLogsKeys.Dashboard.TopPaths)}
            data={stats.topPaths}
            dataKey="count"
            config={topPathsConfig}
            fill="var(--color-primary)"
            yAxisWidth={160}
          />
        )}
      </div>

      {entityData.length > 0 && (
        <EntityTypesChart
          title={t(AuditLogsKeys.Dashboard.EntityTypes)}
          data={entityData}
          config={entityConfig}
        />
      )}

      {stats.hourlyDistribution.length > 0 && (
        <HourlyDistributionChart
          title={t(AuditLogsKeys.Dashboard.HourlyDistribution)}
          data={stats.hourlyDistribution}
          config={hourlyConfig}
        />
      )}
    </PageShell>
  );
}
