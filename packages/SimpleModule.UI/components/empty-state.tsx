import * as React from 'react';
import { cn } from '../lib/utils';

type HeadingLevel = 'h1' | 'h2' | 'h3' | 'h4' | 'h5' | 'h6';

interface EmptyStateProps extends Omit<React.HTMLAttributes<HTMLDivElement>, 'title'> {
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
  /**
   * Semantic level for the title element. Defaults to `h2` because EmptyState
   * commonly nests directly under a PageShell `<h1>`, so `h2` keeps the
   * document outline contiguous. Override when the page has additional
   * heading levels between the page title and this empty state.
   */
  headingLevel?: HeadingLevel;
}

const EmptyState = React.forwardRef<HTMLDivElement, EmptyStateProps>(
  (
    { className, icon, title, description, action, secondaryAction, headingLevel = 'h2', ...props },
    ref,
  ) => {
    const TitleTag = headingLevel;
    return (
      <div ref={ref} className={cn('text-center px-6 py-12 sm:py-16', className)} {...props}>
        {icon && (
          <div className="inline-flex items-center justify-center w-11 h-11 rounded-full bg-surface-sunken text-text-muted mb-5">
            {icon}
          </div>
        )}
        <TitleTag className="text-lg sm:text-xl font-medium tracking-tight text-text font-display">
          {title}
        </TitleTag>
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
    );
  },
);
EmptyState.displayName = 'EmptyState';

export type { EmptyStateProps };
export { EmptyState };
