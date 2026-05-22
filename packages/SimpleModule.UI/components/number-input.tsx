import * as React from 'react';
import { cn } from '../lib/utils';
import { Button } from './button';

type RootHTMLProps = Omit<React.InputHTMLAttributes<HTMLInputElement>, 'value' | 'onChange'>;

interface NumberInputProps extends RootHTMLProps {
  value: number;
  onChange: (next: number) => void;
  /** Step delta for the +/- controls. Defaults to 1. */
  step?: number;
  /** Inclusive minimum value. */
  min?: number;
  /** Inclusive maximum value. */
  max?: number;
  /**
   * Value to restore when the user clears the input and blurs without typing
   * a new number. Defaults to `min` when provided, otherwise `0`.
   */
  emptyFallback?: number;
}

const clamp = (n: number, min?: number, max?: number) => {
  if (typeof min === 'number' && n < min) return min;
  if (typeof max === 'number' && n > max) return max;
  return n;
};

const NumberInput = React.forwardRef<HTMLInputElement, NumberInputProps>(
  (
    { className, value, onChange, step = 1, min, max, disabled, emptyFallback, onBlur, ...props },
    ref,
  ) => {
    // Local string state mirrors `value` so the user can type freely (including
    // an empty string while editing) without the controlled prop snapping the
    // field back. The numeric `value` is the source of truth on render/blur.
    const [display, setDisplay] = React.useState(String(value));
    const valueRef = React.useRef(value);

    React.useEffect(() => {
      if (value !== valueRef.current) {
        setDisplay(String(value));
        valueRef.current = value;
      }
    }, [value]);

    const atMin = typeof min === 'number' && value <= min;
    const atMax = typeof max === 'number' && value >= max;

    const handleInput = (e: React.ChangeEvent<HTMLInputElement>) => {
      const raw = e.target.value;
      setDisplay(raw);
      if (raw === '' || raw === '-') return; // mid-edit; don't emit yet
      const next = Number.parseFloat(raw);
      if (Number.isNaN(next)) return;
      const clamped = clamp(next, min, max);
      valueRef.current = clamped;
      onChange(clamped);
    };

    const handleBlur = (e: React.FocusEvent<HTMLInputElement>) => {
      if (display === '' || display === '-' || Number.isNaN(Number.parseFloat(display))) {
        const restore = emptyFallback ?? min ?? 0;
        setDisplay(String(restore));
        if (restore !== value) {
          valueRef.current = restore;
          onChange(restore);
        }
      }
      onBlur?.(e);
    };

    const setNumeric = (next: number) => {
      const clamped = clamp(next, min, max);
      setDisplay(String(clamped));
      valueRef.current = clamped;
      onChange(clamped);
    };

    return (
      <div className={cn('inline-flex items-stretch', className)}>
        <Button
          type="button"
          variant="secondary"
          size="sm"
          className="rounded-r-none border-r-0 px-3"
          onClick={() => setNumeric(value - step)}
          disabled={disabled || atMin}
          aria-label="Decrement"
        >
          <svg
            aria-hidden="true"
            width="14"
            height="14"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2.5"
            strokeLinecap="round"
            strokeLinejoin="round"
          >
            <line x1="5" y1="12" x2="19" y2="12" />
          </svg>
        </Button>
        <input
          ref={ref}
          type="number"
          inputMode="decimal"
          className={cn(
            'w-20 px-3 py-2 text-center bg-surface border border-border text-sm text-text',
            'transition-all duration-200 outline-none focus:border-primary focus:ring-4 focus:ring-primary-ring',
            'rounded-none',
            // Hide the native spinner so our custom controls are the only affordance.
            '[appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none',
          )}
          value={display}
          onChange={handleInput}
          onBlur={handleBlur}
          step={step}
          min={min}
          max={max}
          disabled={disabled}
          {...props}
        />
        <Button
          type="button"
          variant="secondary"
          size="sm"
          className="rounded-l-none border-l-0 px-3"
          onClick={() => setNumeric(value + step)}
          disabled={disabled || atMax}
          aria-label="Increment"
        >
          <svg
            aria-hidden="true"
            width="14"
            height="14"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2.5"
            strokeLinecap="round"
            strokeLinejoin="round"
          >
            <line x1="12" y1="5" x2="12" y2="19" />
            <line x1="5" y1="12" x2="19" y2="12" />
          </svg>
        </Button>
      </div>
    );
  },
);
NumberInput.displayName = 'NumberInput';

export type { NumberInputProps };
export { NumberInput };
