import * as React from 'react';
import { cva, type VariantProps } from 'class-variance-authority';
import { cn } from '../lib/utils';

const inputVariants = cva(
  'w-full px-4 py-3 bg-surface border rounded-xl text-sm text-text transition-all duration-200 placeholder:text-text-muted outline-none focus:border-primary focus:ring-4 focus:ring-primary-ring',
  {
    variants: {
      variant: {
        default: 'border-border',
        error: 'border-danger focus:border-danger focus:ring-danger-bg',
      },
    },
    defaultVariants: {
      variant: 'default',
    },
  },
);

interface InputProps extends React.InputHTMLAttributes<HTMLInputElement>, VariantProps<typeof inputVariants> {}

const Input = React.forwardRef<HTMLInputElement, InputProps>(({ className, variant, type, ...props }, ref) => {
  return <input type={type} className={cn(inputVariants({ variant, className }))} ref={ref} {...props} />;
});
Input.displayName = 'Input';

export { Input, inputVariants };
export type { InputProps };
