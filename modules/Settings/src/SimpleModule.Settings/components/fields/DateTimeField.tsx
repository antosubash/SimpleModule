import { Button, Input } from '@simplemodule/ui';
import { useState } from 'react';
import { useSavedFlash, validateRequired } from './common';
import type { FieldProps } from './types';

function isoToLocal(iso: string): string {
  if (!iso) return '';
  try {
    const d = new Date(iso);
    if (Number.isNaN(d.getTime())) return '';
    // datetime-local expects "YYYY-MM-DDTHH:mm"
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
  } catch {
    return '';
  }
}

function localToIso(local: string): string {
  if (!local) return '';
  try {
    return new Date(local).toISOString();
  } catch {
    return local;
  }
}

export default function DateTimeField({
  definition,
  value,
  onSave,
  onDirty,
  disabled,
  autoFocus,
}: FieldProps) {
  const isoInitial = typeof value === 'string' ? value : '';
  const [local, setLocal] = useState(isoToLocal(isoInitial));
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [saved, flash] = useSavedFlash();

  const hasChanged = local !== isoToLocal(isoInitial);

  const validate = (): string | null => {
    if (definition.required && !local) return validateRequired(local);
    if (local) {
      const d = new Date(local);
      if (Number.isNaN(d.getTime())) return 'Enter a valid date and time.';
    }
    return null;
  };

  const handleChange = (v: string) => {
    setLocal(v);
    setError(null);
    onDirty?.(v !== isoToLocal(isoInitial));
  };

  const handleSave = async () => {
    const err = validate();
    if (err) {
      setError(err);
      return;
    }
    setSaving(true);
    try {
      await onSave(local ? localToIso(local) : null);
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
          type="datetime-local"
          value={local}
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
