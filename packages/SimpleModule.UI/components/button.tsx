import { Slot } from '@radix-ui/react-slot';
import { cva, type VariantProps } from 'class-variance-authority';
import * as React from 'react';
import { cn } from '../lib/utils';

const buttonVariants = cva(
  'inline-flex items-center justify-center gap-2 rounded-xl text-sm font-semibold transition-all duration-200 active:scale-[0.97] cursor-pointer disabled:pointer-events-none disabled:opacity-50',
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
      },
    },
    defaultVariants: {
      variant: 'primary',
      size: 'default',
    },
  },
);

interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {
  asChild?: boolean;
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant, size, asChild = false, ...props }, ref) => {
    const Comp = asChild ? Slot : 'button';
    return (
      <Comp className={cn(buttonVariants({ variant, size, className }))} ref={ref} {...props} />
    );
  },
);
Button.displayName = 'Button';

export type { ButtonProps };
export { Button, buttonVariants };
