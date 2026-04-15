import * as React from 'react';
import * as RechartsPrimitive from 'recharts';
import { cn } from '../lib/utils';
import { type ChartConfig, ChartContext } from './chart-context';
import { ChartLegendContent } from './chart-legend';
import { ChartTooltipContent } from './chart-tooltip';

export { type ChartConfig, useChart } from './chart-context';

// ---- useContainerSize ----

function useContainerSize(containerRef: React.RefObject<HTMLDivElement | null>) {
  const [size, setSize] = React.useState({ width: 0, height: 0 });

  React.useEffect(() => {
    const el = containerRef.current;
    if (!el) return;

    const observer = new ResizeObserver((entries) => {
      for (const entry of entries) {
        const { width, height } = entry.contentRect;
        setSize((prev) =>
          prev.width === width && prev.height === height ? prev : { width, height },
        );
      }
    });

    observer.observe(el);
    const rect = el.getBoundingClientRect();
    setSize({ width: rect.width, height: rect.height });

    return () => observer.disconnect();
  }, [containerRef]);

  return size;
}

// ---- ChartContainer ----

const ChartContainer = React.forwardRef<
  HTMLDivElement,
  React.ComponentProps<'div'> & {
    config: ChartConfig;
    children: React.ComponentProps<typeof RechartsPrimitive.ResponsiveContainer>['children'];
  }
>(({ id, className, children, config, ...props }, ref) => {
  const uniqueId = React.useId();
  const chartId = `chart-${id || uniqueId.replace(/:/g, '')}`;
  const innerRef = React.useRef<HTMLDivElement>(null);
  const { width, height } = useContainerSize(innerRef);

  return (
    <ChartContext.Provider value={{ config }}>
      <div
        data-chart={chartId}
        ref={(node) => {
          innerRef.current = node;
          if (typeof ref === 'function') ref(node);
          else if (ref) ref.current = node;
        }}
        className={cn(
          'w-full text-xs [&_.recharts-cartesian-axis-tick_text]:fill-text-muted [&_.recharts-cartesian-grid_line[stroke]]:stroke-border/50 [&_.recharts-curve.recharts-tooltip-cursor]:stroke-border [&_.recharts-dot[stroke]]:stroke-transparent [&_.recharts-layer]:outline-none [&_.recharts-polar-grid_[stroke]]:stroke-border [&_.recharts-radial-bar-background-sector]:fill-surface-sunken [&_.recharts-rectangle.recharts-tooltip-cursor]:fill-surface-sunken [&_.recharts-reference-line_[stroke]]:stroke-border [&_.recharts-sector[stroke]]:stroke-transparent [&_.recharts-sector]:outline-none [&_.recharts-surface]:outline-none',
          className,
        )}
        {...props}
      >
        <ChartStyle id={chartId} config={config} />
        {width > 0 && height > 0 ? (
          <RechartsPrimitive.ResponsiveContainer width={width} height={height}>
            {children}
          </RechartsPrimitive.ResponsiveContainer>
        ) : null}
      </div>
    </ChartContext.Provider>
  );
});
ChartContainer.displayName = 'ChartContainer';

// ---- ChartStyle ----

function ChartStyle({ id, config }: { id: string; config: ChartConfig }) {
  const colorConfig = Object.entries(config).filter(([, cfg]) => cfg.color || cfg.theme);

  if (!colorConfig.length) return null;

  return (
    <style
      // biome-ignore lint/security/noDangerouslySetInnerHtml: CSS variable injection from chart config (no user input)
      dangerouslySetInnerHTML={{
        __html: Object.entries({ light: '', dark: '.dark' })
          .map(
            ([theme, prefix]) =>
              `${prefix} [data-chart=${id}] {
${colorConfig
  .map(([key, itemConfig]) => {
    const color = itemConfig.theme?.[theme] || itemConfig.color;
    return color ? `  --color-${key}: ${color};` : null;
  })
  .filter(Boolean)
  .join('\n')}
}`,
          )
          .join('\n'),
      }}
    />
  );
}

const ChartTooltip = RechartsPrimitive.Tooltip;
const ChartLegend = RechartsPrimitive.Legend;

export {
  ChartContainer,
  ChartLegend,
  ChartLegendContent,
  ChartStyle,
  ChartTooltip,
  ChartTooltipContent,
};
