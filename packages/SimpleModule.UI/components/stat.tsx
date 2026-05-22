import { cva, type VariantProps } from 'class-variance-authority';
import * as React from 'react';
import { cn } from '../lib/utils';

const statVariants = cva(
  'bg-surface border border-border rounded-xl sm:rounded-2xl p-4 sm:p-5 transition-all duration-200 text-left',
  {
    variants: {
      interactive: {
        true: 'hover:border-border-strong hover:shadow-md cursor-pointer outline-none focus-visible:ring-4 focus-visible:ring-primary-ring',
        false: '',
      },
      lift: {
        true: 'hover:-translate-y-px',
        false: '',
      },
    },
    defaultVariants: {
      interactive: false,
      lift: false,
    },
  },
);

interface StatProps
  extends Omit<React.HTMLAttributes<HTMLElement>, 'children'>,
    VariantProps<typeof statVariants> {
  /** Headline value, e.g. `14`, `2.7`, `98.4`, `v1.4`. */
  value: React.ReactNode;
  /** Unit suffix shown muted next to the value, e.g. `ms`, `%`. */
  unit?: React.ReactNode;
  /** Small uppercase label below the value. */
  label: React.ReactNode;
  /** Optional trend marker shown next to the value. */
  trend?: 'up' | 'down' | 'flat';
  /** Optional small change text, e.g. "+12% vs last week". */
  change?: React.ReactNode;
}

const TREND_COLOR: Record<NonNullable<StatProps['trend']>, string> = {
  up: 'text-success',
  down: 'text-danger',
  flat: 'text-text-muted',
};

const TrendIcon = ({ trend }: { trend: NonNullable<StatProps['trend']> }) => {
  if (trend === 'up') {
    return (
      <svg
        aria-hidden="true"
        width="14"
        height="14"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="2.2"
        strokeLinecap="round"
        strokeLinejoin="round"
      >
        <polyline points="23 6 13.5 15.5 8.5 10.5 1 18" />
        <polyline points="17 6 23 6 23 12" />
      </svg>
    );
  }
  if (trend === 'down') {
    return (
      <svg
        aria-hidden="true"
        width="14"
        height="14"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="2.2"
        strokeLinecap="round"
        strokeLinejoin="round"
      >
        <polyline points="23 18 13.5 8.5 8.5 13.5 1 6" />
        <polyline points="17 18 23 18 23 12" />
      </svg>
    );
  }
  return (
    <svg
      aria-hidden="true"
      width="14"
      height="14"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2.2"
      strokeLinecap="round"
    >
      <line x1="3" y1="12" x2="21" y2="12" />
    </svg>
  );
};

/**
 * Dashboard tile metric.
 *
 * When `onClick` is provided the Stat renders as a `<button>` so it is fully
 * keyboard-operable and exposes the correct semantic role to assistive tech.
 * Otherwise it renders as a plain `<div>`.
 */
const Stat = React.forwardRef<HTMLElement, StatProps>(
  ({ className, interactive, lift, value, unit, label, trend, change, onClick, ...props }, ref) => {
    const isInteractive = interactive ?? onClick != null;
    const body = (
      <>
        <div className="flex items-baseline gap-2">
          <span className="dash-stat">{value}</span>
          {unit && <span className="text-sm font-normal text-text-muted">{unit}</span>}
          {trend && (
            <span className={cn('inline-flex items-center', TREND_COLOR[trend])}>
              <TrendIcon trend={trend} />
            </span>
          )}
        </div>
        <p className="dash-label">{label}</p>
        {change && <p className="mt-1 text-xs text-text-muted">{change}</p>}
      </>
    );

    if (isInteractive) {
      return (
        <button
          ref={ref as React.Ref<HTMLButtonElement>}
          type="button"
          onClick={onClick as React.MouseEventHandler<HTMLButtonElement>}
          className={cn(statVariants({ interactive: true, lift }), 'w-full', className)}
          {...(props as React.ButtonHTMLAttributes<HTMLButtonElement>)}
        >
          {body}
        </button>
      );
    }

    return (
      <div
        ref={ref as React.Ref<HTMLDivElement>}
        className={cn(statVariants({ interactive: false, lift }), className)}
        {...(props as React.HTMLAttributes<HTMLDivElement>)}
      >
        {body}
      </div>
    );
  },
);
Stat.displayName = 'Stat';

export type { StatProps };
export { Stat, statVariants };
