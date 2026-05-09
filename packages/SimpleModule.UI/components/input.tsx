import { cva, type VariantProps } from 'class-variance-authority';
import * as React from 'react';
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

interface InputProps
  extends React.InputHTMLAttributes<HTMLInputElement>,
    VariantProps<typeof inputVariants> {
  prefix?: React.ReactNode;
  suffix?: React.ReactNode;
  wrapperClassName?: string;
}

const Input = React.forwardRef<HTMLInputElement, InputProps>(
  ({ className, variant, type, prefix, suffix, wrapperClassName, ...props }, ref) => {
    if (prefix == null && suffix == null) {
      return (
        <input
          type={type}
          className={cn(inputVariants({ variant, className }))}
          ref={ref}
          {...props}
        />
      );
    }

    return (
      <div
        className={cn(
          'relative flex items-center w-full',
          // make adornments inherit muted color and not steal focus
          '[&>[data-slot=prefix]]:absolute [&>[data-slot=prefix]]:left-3 [&>[data-slot=prefix]]:text-text-muted [&>[data-slot=prefix]]:pointer-events-none',
          '[&>[data-slot=suffix]]:absolute [&>[data-slot=suffix]]:right-3 [&>[data-slot=suffix]]:text-text-muted',
          wrapperClassName,
        )}
      >
        {prefix != null && <span data-slot="prefix">{prefix}</span>}
        <input
          type={type}
          className={cn(
            inputVariants({ variant }),
            prefix != null && 'pl-10',
            suffix != null && 'pr-10',
            className,
          )}
          ref={ref}
          {...props}
        />
        {suffix != null && <span data-slot="suffix">{suffix}</span>}
      </div>
    );
  },
);
Input.displayName = 'Input';

export type { InputProps };
export { Input, inputVariants };
