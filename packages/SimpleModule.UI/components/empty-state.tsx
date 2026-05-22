import * as React from 'react';
import { cn } from '../lib/utils';

interface EmptyStateProps extends React.HTMLAttributes<HTMLDivElement> {
  /** Icon shown inside a circular surface above the title. */
  icon?: React.ReactNode;
  /** Headline. */
  title: React.ReactNode;
  /** Body copy explaining the empty state and what to try next. */
  description?: React.ReactNode;
  /** Primary call-to-action (renders to the right of any secondary action). */
  action?: React.ReactNode;
  /** Optional secondary action (cancel/clear). */
  secondaryAction?: React.ReactNode;
}

const EmptyState = React.forwardRef<HTMLDivElement, EmptyStateProps>(
  ({ className, icon, title, description, action, secondaryAction, ...props }, ref) => (
    <div ref={ref} className={cn('text-center px-6 py-12 sm:py-16', className)} {...props}>
      {icon && (
        <div className="inline-flex items-center justify-center w-14 h-14 rounded-full bg-surface-sunken text-text-muted mb-6">
          {icon}
        </div>
      )}
      <h3
        className="text-lg sm:text-xl font-medium tracking-tight text-text"
        style={{ fontFamily: 'var(--font-display)' }}
      >
        {title}
      </h3>
      {description && (
        <p className="mt-2 mx-auto max-w-prose text-sm text-text-secondary leading-relaxed">
          {description}
        </p>
      )}
      {(action || secondaryAction) && (
        <div className="mt-7 flex flex-wrap items-center justify-center gap-2">
          {secondaryAction}
          {action}
        </div>
      )}
    </div>
  ),
);
EmptyState.displayName = 'EmptyState';

export type { EmptyStateProps };
export { EmptyState };
