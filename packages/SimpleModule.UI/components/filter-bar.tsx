import * as React from 'react';
import { cn } from '../lib/utils';

interface FilterBarProps extends React.HTMLAttributes<HTMLDivElement> {
  /**
   * Slot for the primary search input. Use the SearchInput component for
   * a consistent affordance.
   */
  search?: React.ReactNode;
  /** Slot for inline filter/sort controls (buttons, popovers, chips). */
  controls?: React.ReactNode;
  /** Slot for primary actions like "New X". Pushed to the right edge. */
  actions?: React.ReactNode;
}

/**
 * FilterBar — the canonical toolbar for data-grid pages.
 *
 * Layout:
 *   [ search                ][ controls ]   [ actions ]
 *                                          ^
 *                                          pushed right via flex-grow spacer
 *
 * Wraps cleanly on mobile (every slot wraps to its own row).
 */
const FilterBar = React.forwardRef<HTMLDivElement, FilterBarProps>(
  ({ className, search, controls, actions, ...props }, ref) => (
    <div
      ref={ref}
      className={cn(
        'flex flex-wrap items-center gap-3 px-5 py-4 border-b border-border',
        className,
      )}
      {...props}
    >
      {search && <div className="min-w-[16rem] flex-1 max-w-md">{search}</div>}
      {controls && <div className="flex flex-wrap items-center gap-2">{controls}</div>}
      <div className="flex-1" />
      {actions && <div className="flex flex-wrap items-center gap-2">{actions}</div>}
    </div>
  ),
);
FilterBar.displayName = 'FilterBar';

export type { FilterBarProps };
export { FilterBar };
