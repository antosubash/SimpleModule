import { cva, type VariantProps } from 'class-variance-authority';
import type * as React from 'react';
import { cn } from '../lib/utils';

const badgeVariants = cva(
  'inline-flex items-center px-2.5 py-1 rounded-full text-xs font-semibold',
  {
    variants: {
      variant: {
        default: 'bg-surface-raised text-text-secondary',
        success: 'bg-success-bg text-success-text',
        danger: 'bg-danger-bg text-danger-text',
        warning: 'bg-warning-bg text-warning-text',
        info: 'bg-info-bg text-primary',
      },
    },
    defaultVariants: {
      variant: 'default',
    },
  },
);

interface BadgeProps
  extends React.HTMLAttributes<HTMLDivElement>,
    VariantProps<typeof badgeVariants> {}

function Badge({ className, variant, ...props }: BadgeProps) {
  return <div className={cn(badgeVariants({ variant }), className)} {...props} />;
}

export { Badge, badgeVariants };
export type { BadgeProps };
