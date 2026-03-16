import { cva, type VariantProps } from 'class-variance-authority';
import type * as React from 'react';
import { cn } from '../lib/utils';

const spinnerVariants = cva(
  'inline-block border-2 border-border border-t-primary rounded-full animate-spin',
  {
    variants: {
      size: {
        sm: 'w-3 h-3',
        default: 'w-4 h-4',
        lg: 'w-6 h-6',
      },
    },
    defaultVariants: {
      size: 'default',
    },
  },
);

interface SpinnerProps
  extends React.HTMLAttributes<HTMLOutputElement>,
    VariantProps<typeof spinnerVariants> {}

function Spinner({ className, size, ...props }: SpinnerProps) {
  return (
    <output className={cn(spinnerVariants({ size }), className)} aria-label="Loading" {...props} />
  );
}

export { Spinner, spinnerVariants };
export type { SpinnerProps };
