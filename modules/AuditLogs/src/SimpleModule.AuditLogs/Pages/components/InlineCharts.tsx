import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  type ChartConfig,
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
} from '@simplemodule/ui';
import { Area, AreaChart, Bar, BarChart, CartesianGrid, Cell, XAxis, YAxis } from 'recharts';
import type { DashboardStats } from '@/types';
import { PALETTE } from './dashboard-constants';

export function TimelineAreaChart({
  title,
  data,
  config,
}: {
  title: string;
  data: DashboardStats['timeline'];
  config: ChartConfig;
}) {
  return (
    <Card>
      <CardHeader className="pb-2">
        <CardTitle className="text-sm font-medium">{title}</CardTitle>
      </CardHeader>
      <CardContent className="p-4 sm:p-6">
        <ChartContainer config={config} className="min-h-[200px] sm:min-h-[250px]">
          <AreaChart data={data} margin={{ top: 4, right: 4, bottom: 0, left: 0 }}>
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
  );
}

export function EntityTypesChart({
  title,
  data,
  config,
}: {
  title: string;
  data: { name: string; value: number }[];
  config: ChartConfig;
}) {
  return (
    <Card>
      <CardHeader className="pb-2">
        <CardTitle className="text-sm font-medium">{title}</CardTitle>
      </CardHeader>
      <CardContent className="p-4 sm:p-6">
        <ChartContainer config={config} className="min-h-[180px] sm:min-h-[200px]">
          <BarChart data={data} margin={{ top: 0, right: 16, bottom: 0, left: 0 }}>
            <CartesianGrid vertical={false} strokeDasharray="3 3" />
            <XAxis dataKey="name" tickLine={false} axisLine={false} tick={{ fontSize: 11 }} />
            <YAxis tickLine={false} axisLine={false} width={40} />
            <ChartTooltip content={<ChartTooltipContent />} />
            <Bar dataKey="value" radius={[4, 4, 0, 0]}>
              {data.map((d, i) => (
                <Cell key={d.name} fill={PALETTE[i % PALETTE.length]} />
              ))}
            </Bar>
          </BarChart>
        </ChartContainer>
      </CardContent>
    </Card>
  );
}

export function HourlyDistributionChart({
  title,
  data,
  config,
}: {
  title: string;
  data: DashboardStats['hourlyDistribution'];
  config: ChartConfig;
}) {
  return (
    <Card>
      <CardHeader className="pb-2">
        <CardTitle className="text-sm font-medium">{title}</CardTitle>
      </CardHeader>
      <CardContent className="p-4 sm:p-6">
        <ChartContainer config={config} className="min-h-[180px] sm:min-h-[200px]">
          <BarChart data={data} margin={{ top: 0, right: 16, bottom: 0, left: 0 }}>
            <CartesianGrid vertical={false} strokeDasharray="3 3" />
            <XAxis dataKey="name" tickLine={false} axisLine={false} tick={{ fontSize: 10 }} />
            <YAxis tickLine={false} axisLine={false} width={40} />
            <ChartTooltip content={<ChartTooltipContent />} />
            <Bar dataKey="count" fill="var(--color-primary)" radius={[4, 4, 0, 0]} />
          </BarChart>
        </ChartContainer>
      </CardContent>
    </Card>
  );
}
