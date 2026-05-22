import { Button, Input } from '@simplemodule/ui';
import { useState } from 'react';
import { useSavedFlash, useSyncedLocal, validateRange, validateRequired } from './common';
import type { FieldProps } from './types';

export default function NumberField({
  definition,
  value,
  onSave,
  onDirty,
  disabled,
  autoFocus,
}: FieldProps) {
  const initial = value !== null && value !== undefined ? String(value) : '';
  const [local, setLocal] = useSyncedLocal(initial);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [saved, flash] = useSavedFlash();

  const hasChanged = local !== initial;

  const validate = (): string | null => {
    if (definition.required && local.trim() === '') return validateRequired(local);
    if (local.trim() !== '') {
      const num = Number(local);
      if (!Number.isFinite(num)) return 'Enter a valid number.';
      const rangeErr = validateRange(num, definition.min, definition.max);
      if (rangeErr) return rangeErr;
    }
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
    const raw = local.trim();
    const num = raw === '' ? null : Number(raw);
    const clamped =
      num !== null && Number.isFinite(num)
        ? Math.min(definition.max ?? num, Math.max(definition.min ?? num, num))
        : num;
    setSaving(true);
    try {
      await onSave(clamped);
      flash();
      onDirty?.(false);
    } finally {
      setSaving(false);
    }
  };

  const hint =
    definition.min !== undefined || definition.max !== undefined ? (
      <span className="text-xs text-muted-foreground">
        {definition.min !== undefined && definition.max !== undefined
          ? `${definition.min} – ${definition.max}`
          : definition.min !== undefined
            ? `min ${definition.min}`
            : `max ${definition.max}`}
      </span>
    ) : null;

  return (
    <div className="space-y-2">
      <div className="flex items-center gap-2">
        <Input
          id={definition.key}
          type="number"
          value={local}
          placeholder={definition.placeholder}
          min={definition.min}
          max={definition.max}
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
      {hint}
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
