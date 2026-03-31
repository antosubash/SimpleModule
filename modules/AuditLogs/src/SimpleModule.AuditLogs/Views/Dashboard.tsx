import { router } from '@inertiajs/react';
import {
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  type ChartConfig,
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
  DatePicker,
  PageShell,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@simplemodule/ui';
import { useState } from 'react';
import {
  Area,
  AreaChart,
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Pie,
  PieChart,
  XAxis,
  YAxis,
} from 'recharts';
import type { DashboardStats, NamedCount } from '../types';

interface Props {
  stats: DashboardStats;
  from: string;
  to: string;
  userId: string;
  users: NamedCount[];
}

// ---- Chart color palette ----

const PALETTE = [
  'var(--color-primary)',
  'var(--color-info)',
  'var(--color-warning)',
  'var(--color-danger)',
  'var(--color-success-light)',
  'var(--color-accent)',
  'var(--color-muted)',
  'var(--color-primary-light)',
];

const SOURCE_COLORS: Record<string, string> = {
  Http: 'var(--color-info)',
  Domain: 'var(--color-primary)',
  ChangeTracker: 'var(--color-warning)',
};

const STATUS_COLORS: Record<string, string> = {
  '2xx': 'var(--color-success)',
  '3xx': 'var(--color-info)',
  '4xx': 'var(--color-warning)',
  '5xx': 'var(--color-danger)',
  Other: 'var(--color-muted)',
};

const DATE_PRESETS = [
  { label: 'Last 24h', hours: 24 },
  { label: 'Last 7 days', hours: 168 },
  { label: 'Last 30 days', hours: 720 },
  { label: 'Last 90 days', hours: 2160 },
];

// ---- Helpers ----

function dictToChartData(dict: Record<string, number>): { name: string; value: number }[] {
  return Object.entries(dict)
    .map(([name, value]) => ({ name, value }))
    .sort((a, b) => b.value - a.value);
}

function formatDate(iso: string): string {
  return iso.slice(0, 10);
}

// ---- KPI Card ----

function KpiCard({
  title,
  value,
  subtitle,
  accent,
  onClick,
}: {
  title: string;
  value: string;
  subtitle?: string;
  accent?: 'default' | 'danger';
  onClick?: () => void;
}) {
  return (
    <Card className={onClick ? 'cursor-pointer transition-shadow hover:shadow-md' : ''}>
      <CardContent className="p-5" onClick={onClick}>
        <p className="text-xs font-medium tracking-wide text-text-muted uppercase">{title}</p>
        <p
          className={`mt-1 text-2xl font-bold tabular-nums ${
            accent === 'danger' ? 'text-danger' : 'text-text'
          }`}
        >
          {value}
        </p>
        {subtitle && <p className="mt-0.5 text-xs text-text-muted">{subtitle}</p>}
      </CardContent>
    </Card>
  );
}

// ---- Reusable chart cards ----

function DonutCard({
  title,
  data,
  colors,
  config,
}: {
  title: string;
  data: { name: string; value: number }[];
  colors: Record<string, string>;
  config: ChartConfig;
}) {
  return (
    <Card className="flex flex-col">
      <CardHeader className="pb-2">
        <CardTitle className="text-sm font-medium">{title}</CardTitle>
      </CardHeader>
      <CardContent className="flex flex-1 flex-col justify-center">
        <ChartContainer config={config} style={{ height: 220, maxWidth: 300, margin: '0 auto' }}>
          <PieChart>
            <ChartTooltip content={<ChartTooltipContent nameKey="name" hideLabel />} />
            <Pie
              data={data}
              dataKey="value"
              nameKey="name"
              cx="50%"
              cy="50%"
              innerRadius={50}
              outerRadius={90}
              strokeWidth={2}
              stroke="var(--color-surface)"
            >
              {data.map((d) => (
                <Cell key={d.name} fill={colors[d.name] || PALETTE[0]} />
              ))}
            </Pie>
          </PieChart>
        </ChartContainer>
        <div className="mt-3 flex justify-center gap-4 text-xs">
          {data.map((d) => (
            <div key={d.name} className="flex items-center gap-1.5">
              <div
                className="h-2 w-2 rounded-full"
                style={{ backgroundColor: colors[d.name] || PALETTE[0] }}
              />
              <span className="text-text-muted">{d.name}</span>
              <span className="font-medium tabular-nums">{d.value.toLocaleString()}</span>
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}

function HBarCard({
  title,
  data,
  dataKey,
  config,
  fill,
  yAxisWidth = 100,
}: {
  title: string;
  data: readonly { name: string; count?: number; value?: number }[];
  dataKey: string;
  config: ChartConfig;
  fill?: string;
  yAxisWidth?: number;
}) {
  return (
    <Card className="flex flex-col">
      <CardHeader className="pb-2">
        <CardTitle className="text-sm font-medium">{title}</CardTitle>
      </CardHeader>
      <CardContent className="flex-1">
        <ChartContainer config={config} style={{ height: 280 }}>
          <BarChart
            data={data}
            layout="vertical"
            margin={{ top: 0, right: 16, bottom: 0, left: 0 }}
          >
            <CartesianGrid horizontal={false} strokeDasharray="3 3" />
            <YAxis
              dataKey="name"
              type="category"
              tickLine={false}
              axisLine={false}
              width={yAxisWidth}
              tick={{ fontSize: 11 }}
            />
            <XAxis type="number" tickLine={false} axisLine={false} />
            <ChartTooltip content={<ChartTooltipContent />} />
            <Bar dataKey={dataKey} fill={fill} radius={[0, 4, 4, 0]}>
              {!fill &&
                data.map((d, i) => (
                  <Cell key={d.name as string} fill={PALETTE[i % PALETTE.length]} />
                ))}
            </Bar>
          </BarChart>
        </ChartContainer>
      </CardContent>
    </Card>
  );
}

// ---- Main Dashboard ----

export default function Dashboard({ stats, from, to, userId, users }: Props) {
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
      className="space-y-4"
      title="Audit Dashboard"
      description="System activity overview and metrics"
      actions={
        <div className="flex flex-wrap items-end gap-2">
          {/* Quick date presets */}
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
          <div className="space-y-1">
            <span className="text-xs font-medium text-text-muted">From</span>
            <DatePicker value={dateFrom} onChange={setDateFrom} placeholder="Start date" />
          </div>
          <div className="space-y-1">
            <span className="text-xs font-medium text-text-muted">To</span>
            <DatePicker value={dateTo} onChange={setDateTo} placeholder="End date" />
          </div>
          <div className="space-y-1">
            <span className="text-xs font-medium text-text-muted">User</span>
            <Select value={selectedUser} onValueChange={setSelectedUser}>
              <SelectTrigger className="w-[180px]" aria-label="User">
                <SelectValue placeholder="All users" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="__all__">All users</SelectItem>
                {users.map((u) => (
                  <SelectItem key={u.name} value={u.name}>
                    {u.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <Button onClick={applyFilters}>Apply</Button>
        </div>
      }
    >
      {/* KPI Cards */}
      <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
        <KpiCard
          title="Total Events"
          value={stats.totalEntries.toLocaleString()}
          subtitle={`${formatDate(from)} \u2014 ${formatDate(to)}`}
          onClick={() => browseWithFilter({})}
        />
        <KpiCard title="Unique Users" value={stats.uniqueUsers.toLocaleString()} />
        <KpiCard
          title="Avg Response"
          value={stats.averageDurationMs > 0 ? `${stats.averageDurationMs}ms` : '\u2014'}
          subtitle="HTTP requests"
          onClick={() => browseWithFilter({ source: '0' })}
        />
        <KpiCard
          title="Error Rate"
          value={stats.errorRate > 0 ? `${stats.errorRate}%` : '0%'}
          accent={stats.errorRate > 5 ? 'danger' : 'default'}
          subtitle="4xx + 5xx responses"
          onClick={() => browseWithFilter({})}
        />
      </div>

      {/* Timeline Area Chart */}
      {stats.timeline.length > 0 && (
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">Activity Timeline</CardTitle>
          </CardHeader>
          <CardContent>
            <ChartContainer config={timelineConfig} style={{ height: 250 }}>
              <AreaChart data={stats.timeline} margin={{ top: 4, right: 4, bottom: 0, left: 0 }}>
                <defs>
                  <linearGradient id="fillHttp" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="0%" stopColor="var(--color-http)" stopOpacity={0.3} />
                    <stop offset="100%" stopColor="var(--color-http)" stopOpacity={0.02} />
                  </linearGradient>
                  <linearGradient id="fillDomain" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="0%" stopColor="var(--color-domain)" stopOpacity={0.3} />
                    <stop offset="100%" stopColor="var(--color-domain)" stopOpacity={0.02} />
                  </linearGradient>
                  <linearGradient id="fillChanges" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="0%" stopColor="var(--color-changes)" stopOpacity={0.3} />
                    <stop offset="100%" stopColor="var(--color-changes)" stopOpacity={0.02} />
                  </linearGradient>
                </defs>
                <CartesianGrid vertical={false} strokeDasharray="3 3" />
                <XAxis
                  dataKey="date"
                  tickLine={false}
                  axisLine={false}
                  tickMargin={8}
                  tickFormatter={(v: string) => v.slice(5)}
                />
                <YAxis tickLine={false} axisLine={false} tickMargin={4} width={40} />
                <ChartTooltip content={<ChartTooltipContent />} />
                <Area
                  dataKey="http"
                  type="monotone"
                  fill="url(#fillHttp)"
                  stroke="var(--color-http)"
                  strokeWidth={2}
                  stackId="a"
                />
                <Area
                  dataKey="domain"
                  type="monotone"
                  fill="url(#fillDomain)"
                  stroke="var(--color-domain)"
                  strokeWidth={2}
                  stackId="a"
                />
                <Area
                  dataKey="changes"
                  type="monotone"
                  fill="url(#fillChanges)"
                  stroke="var(--color-changes)"
                  strokeWidth={2}
                  stackId="a"
                />
              </AreaChart>
            </ChartContainer>
          </CardContent>
        </Card>
      )}

      {/* Row: Source Pie + Action Bar */}
      <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
        {sourceData.length > 0 && (
          <DonutCard
            title="By Source"
            data={sourceData}
            colors={SOURCE_COLORS}
            config={sourceConfig}
          />
        )}
        {actionData.length > 0 && (
          <HBarCard title="By Action" data={actionData} dataKey="value" config={actionConfig} />
        )}
      </div>

      {/* Row: Status Pie + Module Bar */}
      <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
        {statusData.length > 0 && (
          <DonutCard
            title="Status Codes"
            data={statusData}
            colors={STATUS_COLORS}
            config={statusConfig}
          />
        )}
        {moduleData.length > 0 && (
          <HBarCard title="By Module" data={moduleData} dataKey="value" config={moduleConfig} />
        )}
      </div>

      {/* Row: Top Users + Top Paths */}
      <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
        {stats.topUsers.length > 0 && (
          <HBarCard
            title="Top Users"
            data={stats.topUsers}
            dataKey="count"
            config={topUsersConfig}
            fill="var(--color-info)"
            yAxisWidth={120}
          />
        )}
        {stats.topPaths.length > 0 && (
          <HBarCard
            title="Top Paths"
            data={stats.topPaths}
            dataKey="count"
            config={topPathsConfig}
            fill="var(--color-primary)"
            yAxisWidth={160}
          />
        )}
      </div>

      {/* Entity Types */}
      {entityData.length > 0 && (
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">Entity Types</CardTitle>
          </CardHeader>
          <CardContent>
            <ChartContainer config={entityConfig} style={{ height: 200 }}>
              <BarChart data={entityData} margin={{ top: 0, right: 16, bottom: 0, left: 0 }}>
                <CartesianGrid vertical={false} strokeDasharray="3 3" />
                <XAxis dataKey="name" tickLine={false} axisLine={false} tick={{ fontSize: 11 }} />
                <YAxis tickLine={false} axisLine={false} width={40} />
                <ChartTooltip content={<ChartTooltipContent />} />
                <Bar dataKey="value" radius={[4, 4, 0, 0]}>
                  {entityData.map((d, i) => (
                    <Cell key={d.name} fill={PALETTE[i % PALETTE.length]} />
                  ))}
                </Bar>
              </BarChart>
            </ChartContainer>
          </CardContent>
        </Card>
      )}

      {/* Hourly Distribution */}
      {stats.hourlyDistribution.length > 0 && (
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">Hourly Activity Distribution</CardTitle>
          </CardHeader>
          <CardContent>
            <ChartContainer config={hourlyConfig} style={{ height: 200 }}>
              <BarChart
                data={stats.hourlyDistribution}
                margin={{ top: 0, right: 16, bottom: 0, left: 0 }}
              >
                <CartesianGrid vertical={false} strokeDasharray="3 3" />
                <XAxis dataKey="name" tickLine={false} axisLine={false} tick={{ fontSize: 10 }} />
                <YAxis tickLine={false} axisLine={false} width={40} />
                <ChartTooltip content={<ChartTooltipContent />} />
                <Bar dataKey="count" fill="var(--color-primary)" radius={[4, 4, 0, 0]} />
              </BarChart>
            </ChartContainer>
          </CardContent>
        </Card>
      )}
    </PageShell>
  );
}
