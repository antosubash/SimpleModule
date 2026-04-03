import * as React from 'react';
import { cn } from '../lib/utils';

interface PageHeaderProps extends Omit<React.HTMLAttributes<HTMLDivElement>, 'title'> {
  title: React.ReactNode;
  description?: string;
  actions?: React.ReactNode;
}

const PageHeader = React.forwardRef<HTMLDivElement, PageHeaderProps>(
  ({ className, title, description, actions, ...props }, ref) => (
    <div
      ref={ref}
      className={cn(
        'flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between sm:gap-4 mb-6',
        className,
      )}
      {...props}
    >
      <div className="min-w-0">
        <h1
          className="text-xl sm:text-2xl font-extrabold tracking-tight"
          style={{ fontFamily: "'Sora', sans-serif" }}
        >
          <span className="gradient-text">{title}</span>
        </h1>
        {description && <p className="text-text-muted text-sm mt-1">{description}</p>}
      </div>
      {actions && <div className="flex items-center gap-2 shrink-0">{actions}</div>}
    </div>
  ),
);
PageHeader.displayName = 'PageHeader';

export type { PageHeaderProps };
export { PageHeader };
