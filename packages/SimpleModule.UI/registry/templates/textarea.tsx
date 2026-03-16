import * as React from 'react';
import { cn } from '../lib/utils';

interface TextareaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {}

const Textarea = React.forwardRef<HTMLTextAreaElement, TextareaProps>(
  ({ className, ...props }, ref) => {
    return (
      <textarea
        className={cn(
          'w-full px-4 py-3 bg-surface border border-border rounded-xl text-sm text-text transition-all duration-200 placeholder:text-text-muted outline-none focus:border-primary focus:ring-4 focus:ring-primary-ring min-h-[80px] resize-y',
          className,
        )}
        ref={ref}
        {...props}
      />
    );
  },
);
Textarea.displayName = 'Textarea';

export { Textarea };
export type { TextareaProps };
