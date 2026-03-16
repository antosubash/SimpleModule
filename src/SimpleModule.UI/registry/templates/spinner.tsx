import * as React from 'react';
import { cva, type VariantProps } from 'class-variance-authority';
import { cn } from '../lib/utils';

const spinnerVariants = cva('inline-block border-2 border-border border-t-primary rounded-full animate-spin', {
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
});

interface SpinnerProps extends React.HTMLAttributes<HTMLDivElement>, VariantProps<typeof spinnerVariants> {}

function Spinner({ className, size, ...props }: SpinnerProps) {
  return <div className={cn(spinnerVariants({ size }), className)} role="status" aria-label="Loading" {...props} />;
}

export { Spinner, spinnerVariants };
export type { SpinnerProps };
