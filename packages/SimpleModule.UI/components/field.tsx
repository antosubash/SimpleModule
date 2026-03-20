import * as React from 'react';
import { cn } from '../lib/utils';

const Field = React.forwardRef<
  HTMLDivElement,
  React.HTMLAttributes<HTMLDivElement> & { orientation?: 'vertical' | 'horizontal' }
>(({ className, orientation = 'vertical', ...props }, ref) => (
  <div
    ref={ref}
    className={cn(
      orientation === 'horizontal' ? 'flex items-center justify-between gap-4' : 'space-y-2',
      className,
    )}
    {...props}
  />
));
Field.displayName = 'Field';

const FieldGroup = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement>>(
  ({ className, ...props }, ref) => (
    <div ref={ref} className={cn('space-y-4', className)} {...props} />
  ),
);
FieldGroup.displayName = 'FieldGroup';

const FieldDescription = React.forwardRef<
  HTMLParagraphElement,
  React.HTMLAttributes<HTMLParagraphElement>
>(({ className, ...props }, ref) => (
  <p ref={ref} className={cn('text-xs text-text-muted', className)} {...props} />
));
FieldDescription.displayName = 'FieldDescription';

const FieldError = React.forwardRef<
  HTMLParagraphElement,
  React.HTMLAttributes<HTMLParagraphElement>
>(({ className, ...props }, ref) => (
  <p ref={ref} className={cn('text-xs text-danger font-medium', className)} {...props} />
));
FieldError.displayName = 'FieldError';

export { Field, FieldDescription, FieldError, FieldGroup };
