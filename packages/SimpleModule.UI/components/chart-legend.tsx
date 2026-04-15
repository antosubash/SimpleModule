import * as React from 'react';
import { cn } from '../lib/utils';
import { getPayloadConfigFromPayload, useChart } from './chart-context';

export const ChartLegendContent = React.forwardRef<
  HTMLDivElement,
  // biome-ignore lint/suspicious/noExplicitAny: recharts tooltip/legend types are incompatible with strict TS 6
  any
>(
  (
    {
      className,
      hideIcon = false,
      payload,
      verticalAlign = 'bottom',
      nameKey,
    }: {
      className?: string;
      hideIcon?: boolean;
      payload?: Array<Record<string, unknown>>;
      verticalAlign?: 'top' | 'bottom';
      nameKey?: string;
    },
    ref: React.Ref<HTMLDivElement>,
  ) => {
    const { config } = useChart();

    if (!payload?.length) return null;

    return (
      <div
        ref={ref}
        className={cn(
          'flex items-center justify-center gap-4',
          verticalAlign === 'top' ? 'pb-3' : 'pt-3',
          className,
        )}
      >
        {payload.map((item: Record<string, unknown>) => {
          const key = `${nameKey || item.dataKey || 'value'}`;
          const itemConfig = getPayloadConfigFromPayload(config, item, key);

          return (
            <div
              key={`${key}-${String(item.value)}`}
              className={cn(
                'flex items-center gap-1.5 [&>svg]:h-3 [&>svg]:w-3 [&>svg]:text-text-muted',
              )}
            >
              {itemConfig?.icon && !hideIcon ? (
                <itemConfig.icon />
              ) : (
                <div
                  className="h-2 w-2 shrink-0 rounded-[2px]"
                  style={{ backgroundColor: item.color as string }}
                />
              )}
              {itemConfig?.label as React.ReactNode}
            </div>
          );
        })}
      </div>
    );
  },
);
ChartLegendContent.displayName = 'ChartLegendContent';
