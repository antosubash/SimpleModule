import * as React from 'react';
import { cn } from '../lib/utils';

interface ResizablePanelGroupProps extends React.HTMLAttributes<HTMLDivElement> {
  direction?: 'horizontal' | 'vertical';
}

const ResizablePanelGroup = React.forwardRef<HTMLDivElement, ResizablePanelGroupProps>(
  ({ className, direction = 'horizontal', ...props }, ref) => (
    <div
      ref={ref}
      data-direction={direction}
      className={cn(
        'flex h-full w-full data-[direction=vertical]:flex-col',
        className,
      )}
      {...props}
    />
  ),
);
ResizablePanelGroup.displayName = 'ResizablePanelGroup';

interface ResizablePanelProps extends React.HTMLAttributes<HTMLDivElement> {
  defaultSize?: number;
  minSize?: number;
  maxSize?: number;
}

const ResizablePanel = React.forwardRef<HTMLDivElement, ResizablePanelProps>(
  ({ className, defaultSize, minSize, maxSize, style, ...props }, ref) => (
    <div
      ref={ref}
      className={cn('flex-1 overflow-auto', className)}
      style={{
        flex: defaultSize ? `${defaultSize} 1 0%` : undefined,
        minWidth: minSize ? `${minSize}%` : undefined,
        maxWidth: maxSize ? `${maxSize}%` : undefined,
        ...style,
      }}
      {...props}
    />
  ),
);
ResizablePanel.displayName = 'ResizablePanel';

interface ResizableHandleProps extends React.HTMLAttributes<HTMLDivElement> {
  withHandle?: boolean;
}

const ResizableHandle = React.forwardRef<HTMLDivElement, ResizableHandleProps>(
  ({ className, withHandle, ...props }, ref) => (
    <div
      ref={ref}
      className={cn(
        'relative flex w-px items-center justify-center bg-border after:absolute after:inset-y-0 after:left-1/2 after:w-1 after:-translate-x-1/2 focus-visible:outline-none focus-visible:ring-4 focus-visible:ring-primary-ring [[data-direction=vertical]_&]:h-px [[data-direction=vertical]_&]:w-full [[data-direction=vertical]_&]:after:left-0 [[data-direction=vertical]_&]:after:h-1 [[data-direction=vertical]_&]:after:w-full [[data-direction=vertical]_&]:after:-translate-y-1/2 [[data-direction=vertical]_&]:after:translate-x-0',
        className,
      )}
      {...props}
    >
      {withHandle && (
        <div className="z-10 flex h-4 w-3 items-center justify-center rounded-sm border border-border bg-surface">
          <svg className="h-2.5 w-2.5 text-text-muted" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24" aria-hidden="true">
            <circle cx="9" cy="12" r="1" fill="currentColor" />
            <circle cx="15" cy="12" r="1" fill="currentColor" />
          </svg>
        </div>
      )}
    </div>
  ),
);
ResizableHandle.displayName = 'ResizableHandle';

export { ResizablePanelGroup, ResizablePanel, ResizableHandle };
export type { ResizablePanelGroupProps, ResizablePanelProps, ResizableHandleProps };
