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
import { Bar, BarChart, CartesianGrid, Cell, Pie, PieChart, XAxis, YAxis } from 'recharts';
import { PALETTE } from './dashboard-constants';

export function DonutCard({
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
      <CardContent className="flex flex-1 flex-col justify-center p-4 sm:p-6">
        <ChartContainer
          config={config}
          className="min-h-[180px] sm:min-h-[220px]"
          style={{ maxWidth: 300, margin: '0 auto' }}
        >
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

export function HBarCard({
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
      <CardContent className="flex-1 p-4 sm:p-6">
        <ChartContainer config={config} className="min-h-[220px] sm:min-h-[280px]">
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
