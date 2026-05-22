import * as React from 'react';
import { cn } from '../lib/utils';

interface KbdProps extends React.HTMLAttributes<HTMLElement> {}

const Kbd = React.forwardRef<HTMLElement, KbdProps>(({ className, children, ...props }, ref) => (
  <kbd
    ref={ref}
    className={cn(
      'inline-flex items-center justify-center min-w-[1.25rem] h-[1.25rem] px-1.5',
      'font-mono text-[0.65rem] font-medium',
      'bg-surface-sunken text-text-muted',
      'border border-border rounded',
      'select-none',
      className,
    )}
    {...props}
  >
    {children}
  </kbd>
));
Kbd.displayName = 'Kbd';

export type { KbdProps };
export { Kbd };
