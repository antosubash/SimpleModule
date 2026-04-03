import { cva, type VariantProps } from 'class-variance-authority';
import * as React from 'react';
import { cn } from '../lib/utils';

const containerVariants = cva('mx-auto w-full px-4 py-4 sm:px-6 sm:py-6 lg:px-8', {
  variants: {
    size: {
      sm: 'max-w-3xl',
      default: 'max-w-7xl',
      lg: 'max-w-7xl',
      full: 'max-w-full',
    },
  },
  defaultVariants: {
    size: 'default',
  },
});

interface ContainerProps
  extends React.HTMLAttributes<HTMLDivElement>,
    VariantProps<typeof containerVariants> {}

const Container = React.forwardRef<HTMLDivElement, ContainerProps>(
  ({ className, size, ...props }, ref) => (
    <div ref={ref} className={cn(containerVariants({ size, className }))} {...props} />
  ),
);
Container.displayName = 'Container';

export type { ContainerProps };
export { Container, containerVariants };
