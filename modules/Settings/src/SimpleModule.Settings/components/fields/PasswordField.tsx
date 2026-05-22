import { Button, Input } from '@simplemodule/ui';
import { useState } from 'react';
import { useSavedFlash, useSyncedLocal, validateRequired } from './common';
import type { FieldProps } from './types';

export default function PasswordField({
  definition,
  value,
  onSave,
  onDirty,
  disabled,
  autoFocus,
}: FieldProps) {
  const hasExistingValue = definition.sensitive ? value !== null && value !== undefined : !!value;
  const initial = definition.sensitive ? '' : typeof value === 'string' ? value : '';

  const [local, setLocal] = useSyncedLocal(initial);
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [saved, flash] = useSavedFlash();

  const hasChanged = local !== initial;

  const validate = (): string | null => {
    if (definition.required && !local) return validateRequired(local);
    return null;
  };

  const handleChange = (v: string) => {
    setLocal(v);
    setError(null);
    onDirty?.(v !== initial);
  };

  const handleSave = async () => {
    const err = validate();
    if (err) {
      setError(err);
      return;
    }
    setSaving(true);
    try {
      await onSave(local);
      flash();
      onDirty?.(false);
    } finally {
      setSaving(false);
    }
  };

  const placeholder =
    definition.sensitive && hasExistingValue
      ? 'Enter new value to replace'
      : (definition.placeholder ?? '');

  return (
    <div className="space-y-2">
      {definition.sensitive && hasExistingValue && !local && (
        <p className="text-sm text-muted-foreground">
          &#8226;&#8226;&#8226;&#8226;&#8226;&#8226;&#8226; (set)
        </p>
      )}
      <div className="flex items-center gap-2">
        <div className="relative flex-1">
          <Input
            id={definition.key}
            type={showPassword ? 'text' : 'password'}
            value={local}
            placeholder={placeholder}
            disabled={disabled || saving}
            autoFocus={autoFocus}
            aria-invalid={!!error}
            aria-describedby={error ? `${definition.key}-error` : undefined}
            onChange={(e) => handleChange(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === 'Enter' && hasChanged) void handleSave();
            }}
            className="pr-10"
          />
          <button
            type="button"
            onClick={() => setShowPassword((v) => !v)}
            className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-text"
            aria-label={showPassword ? 'Hide password' : 'Show password'}
          >
            {showPassword ? (
              <svg
                aria-hidden="true"
                className="h-4 w-4"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                viewBox="0 0 24 24"
              >
                <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94" />
                <path d="M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19" />
                <line x1="1" y1="1" x2="23" y2="23" />
              </svg>
            ) : (
              <svg
                aria-hidden="true"
                className="h-4 w-4"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                viewBox="0 0 24 24"
              >
                <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" />
                <circle cx="12" cy="12" r="3" />
              </svg>
            )}
          </button>
        </div>
        {saved && <span className="text-sm text-green-600">&#10003;</span>}
      </div>
      {error && (
        <p id={`${definition.key}-error`} className="text-sm text-destructive">
          {error}
        </p>
      )}
      {hasChanged && (
        <Button size="sm" onClick={() => void handleSave()} disabled={disabled || saving}>
          {saving ? 'Saving...' : 'Save'}
        </Button>
      )}
    </div>
  );
}
