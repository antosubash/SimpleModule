import * as LabelPrimitive from '@radix-ui/react-label';
import * as React from 'react';
import { cn } from '../lib/utils';

const Label = React.forwardRef<
  React.ComponentRef<typeof LabelPrimitive.Root>,
  React.ComponentPropsWithoutRef<typeof LabelPrimitive.Root>
>(({ className, ...props }, ref) => (
  <LabelPrimitive.Root
    ref={ref}
    className={cn('block mb-1.5 font-medium text-sm text-text-secondary', className)}
    {...props}
  />
));
Label.displayName = 'Label';

export { Label };
