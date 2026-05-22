import { Button, Input } from '@simplemodule/ui';
import { useState } from 'react';
import { useSavedFlash, useSyncedLocal, validateEmail, validateRequired } from './common';
import type { FieldProps } from './types';

export default function EmailField({
  definition,
  value,
  onSave,
  onDirty,
  disabled,
  autoFocus,
}: FieldProps) {
  const initial = typeof value === 'string' ? value : '';
  const [local, setLocal] = useSyncedLocal(initial);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [saved, flash] = useSavedFlash();

  const hasChanged = local !== initial;

  const validate = (): string | null => {
    if (definition.required && !local) return validateRequired(local);
    if (local) return validateEmail(local);
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

  return (
    <div className="space-y-2">
      <div className="flex items-center gap-2">
        <Input
          id={definition.key}
          type="email"
          value={local}
          placeholder={definition.placeholder ?? 'user@example.com'}
          disabled={disabled || saving}
          autoFocus={autoFocus}
          aria-invalid={!!error}
          aria-describedby={error ? `${definition.key}-error` : undefined}
          onChange={(e) => handleChange(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter' && hasChanged) void handleSave();
          }}
          onBlur={() => {
            const err = validate();
            if (err) setError(err);
          }}
          className="flex-1"
        />
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
