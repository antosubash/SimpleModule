import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Badge,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  type ChartConfig,
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
  PageShell,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';
import { Bar, BarChart, CartesianGrid, XAxis, YAxis } from 'recharts';
import { EmailKeys } from '../Locales/keys';
import type { EmailStats } from '../types';

interface Props {
  stats: EmailStats;
}

function failureRateVariant(rate: number): 'default' | 'secondary' | 'destructive' {
  if (rate < 5) return 'default';
  if (rate <= 15) return 'secondary';
  return 'destructive';
}

export default function Dashboard({ stats }: Props) {
  const { t } = useTranslation('Email');

  const chartConfig: ChartConfig = {
    sent: { label: t(EmailKeys.Dashboard.Sent), color: 'var(--color-primary)' },
    failed: { label: t(EmailKeys.Dashboard.Failed), color: 'var(--color-danger)' },
  };

  return (
    <PageShell
      className="space-y-4 sm:space-y-6"
      title={t(EmailKeys.Dashboard.Title)}
      description={t(EmailKeys.Dashboard.Description)}
    >
      {/* Summary Cards */}
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-2 sm:gap-4 lg:grid-cols-4">
        <Card>
          <CardContent className="p-4 sm:p-5">
            <p className="text-xs font-medium tracking-wide text-text-muted uppercase">
              {t(EmailKeys.Dashboard.TotalSent)}
            </p>
            <p className="mt-1 text-xl font-bold tabular-nums sm:text-2xl">
              {stats.totalSent.toLocaleString()}
            </p>
            <p className="mt-0.5 text-xs text-text-muted">
              {stats.sentLast24Hours.toLocaleString()} {t(EmailKeys.Dashboard.Last24Hours)}
            </p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4 sm:p-5">
            <p className="text-xs font-medium tracking-wide text-text-muted uppercase">
              {t(EmailKeys.Dashboard.TotalFailed)}
            </p>
            <p className="mt-1 text-xl font-bold tabular-nums text-danger sm:text-2xl">
              {stats.totalFailed.toLocaleString()}
            </p>
            <p className="mt-0.5 text-xs text-text-muted">
              {stats.failedLast24Hours.toLocaleString()} {t(EmailKeys.Dashboard.Last24Hours)}
            </p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4 sm:p-5">
            <p className="text-xs font-medium tracking-wide text-text-muted uppercase">
              {t(EmailKeys.Dashboard.TotalQueued)}
            </p>
            <p className="mt-1 text-xl font-bold tabular-nums sm:text-2xl">
              {stats.totalQueued.toLocaleString()}
            </p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4 sm:p-5">
            <p className="text-xs font-medium tracking-wide text-text-muted uppercase">
              {t(EmailKeys.Dashboard.TotalRetrying)}
            </p>
            <p className="mt-1 text-xl font-bold tabular-nums sm:text-2xl">
              {stats.totalRetrying.toLocaleString()}
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Failure Rate */}
      <Card>
        <CardContent className="flex items-center gap-3 p-4 sm:p-5">
          <span className="text-sm font-medium text-text">
            {t(EmailKeys.Dashboard.FailureRate)}
          </span>
          <Badge variant={failureRateVariant(stats.failureRateLast7Days)}>
            {stats.failureRateLast7Days.toFixed(1)}%
          </Badge>
        </CardContent>
      </Card>

      {/* Daily Volume Chart */}
      {stats.dailyVolume.length > 0 && (
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">
              {t(EmailKeys.Dashboard.DailyVolume)}
            </CardTitle>
          </CardHeader>
          <CardContent className="p-4 sm:p-6">
            <ChartContainer config={chartConfig} className="min-h-[200px] sm:min-h-[250px]">
              <BarChart data={stats.dailyVolume} margin={{ top: 4, right: 4, bottom: 0, left: 0 }}>
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
                <Bar dataKey="sent" fill="var(--color-sent)" radius={[4, 4, 0, 0]} />
                <Bar dataKey="failed" fill="var(--color-failed)" radius={[4, 4, 0, 0]} />
              </BarChart>
            </ChartContainer>
          </CardContent>
        </Card>
      )}

      {/* Top Errors Table */}
      <Card>
        <CardHeader className="pb-2">
          <CardTitle className="text-sm font-medium">
            {t(EmailKeys.Dashboard.TopErrorsTitle)}
          </CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          {stats.topErrors.length === 0 ? (
            <p className="px-6 py-8 text-center text-sm text-text-muted">
              {t(EmailKeys.Dashboard.TopErrorsEmpty)}
            </p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t(EmailKeys.Dashboard.TopErrorsErrorMessage)}</TableHead>
                  <TableHead className="w-[100px]">
                    {t(EmailKeys.Dashboard.TopErrorsCount)}
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {stats.topErrors.map((err) => (
                  <TableRow key={err.errorMessage}>
                    <TableCell className="text-sm">{err.errorMessage}</TableCell>
                    <TableCell className="font-medium tabular-nums">
                      {err.count.toLocaleString()}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </PageShell>
  );
}
