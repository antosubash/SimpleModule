import { Slot } from '@radix-ui/react-slot';
import { cva, type VariantProps } from 'class-variance-authority';
import * as React from 'react';
import { cn } from '../lib/utils';

const buttonVariants = cva(
  'inline-flex items-center justify-center gap-2 rounded-xl text-sm font-semibold transition-all duration-200 active:scale-[0.97] cursor-pointer disabled:pointer-events-none disabled:opacity-50 outline-none focus-visible:ring-4 focus-visible:ring-primary-ring',
  {
    variants: {
      variant: {
        primary:
          'text-white bg-gradient-to-br from-primary to-accent shadow-(--shadow-primary) hover:shadow-(--shadow-primary-hover) hover:-translate-y-px',
        secondary:
          'bg-surface text-text border border-border hover:bg-surface-raised hover:border-border-strong',
        ghost: 'bg-transparent text-text-secondary hover:bg-primary-subtle hover:text-primary',
        danger:
          'text-white bg-danger shadow-(--shadow-danger) hover:bg-danger-hover hover:shadow-(--shadow-danger-hover) hover:-translate-y-px',
        outline:
          'bg-transparent text-primary border-2 border-primary/30 hover:bg-primary-subtle hover:border-primary',
      },
      size: {
        sm: 'px-3.5 py-1.5 text-xs rounded-lg',
        default: 'px-5 py-2.5',
        lg: 'px-8 py-3.5 text-base',
        icon: 'h-9 w-9 p-0',
      },
    },
    defaultVariants: {
      variant: 'primary',
      size: 'default',
    },
  },
);

const spinnerSizeMap = {
  sm: 'w-3 h-3',
  default: 'w-4 h-4',
  lg: 'w-4 h-4',
  icon: 'w-4 h-4',
} as const;

interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {
  asChild?: boolean;
  isLoading?: boolean;
  loadingText?: React.ReactNode;
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  (
    {
      className,
      variant,
      size,
      asChild = false,
      isLoading = false,
      loadingText,
      disabled,
      children,
      ...props
    },
    ref,
  ) => {
    const Comp = asChild ? Slot : 'button';
    const spinnerClass = spinnerSizeMap[size ?? 'default'];

    if (asChild) {
      return (
        <Comp
          className={cn(buttonVariants({ variant, size, className }))}
          ref={ref}
          aria-busy={isLoading || undefined}
          {...props}
        >
          {children}
        </Comp>
      );
    }

    return (
      <Comp
        className={cn(buttonVariants({ variant, size, className }))}
        ref={ref}
        disabled={disabled || isLoading}
        aria-busy={isLoading || undefined}
        {...props}
      >
        {isLoading ? (
          <>
            <span
              aria-hidden="true"
              className={cn(
                'inline-block border-2 border-current/30 border-t-current rounded-full animate-spin',
                spinnerClass,
              )}
            />
            {loadingText ?? children}
          </>
        ) : (
          children
        )}
      </Comp>
    );
  },
);
Button.displayName = 'Button';

export type { ButtonProps };
export { Button, buttonVariants };
