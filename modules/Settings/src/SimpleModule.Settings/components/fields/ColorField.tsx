import { Button, Input } from '@simplemodule/ui';
import { useState } from 'react';
import { useSavedFlash, validateColor, validateRequired } from './common';
import type { FieldProps } from './types';

export default function ColorField({ definition, value, onSave, onDirty, disabled }: FieldProps) {
  const initial = typeof value === 'string' ? value : '#000000';
  const [local, setLocal] = useState(initial);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [saved, flash] = useSavedFlash();

  const hasChanged = local !== initial;

  const validate = (): string | null => {
    if (definition.required && !local) return validateRequired(local);
    return validateColor(local);
  };

  const handleHexChange = (v: string) => {
    const normalized = v.startsWith('#') ? v : `#${v}`;
    setLocal(normalized);
    setError(null);
    onDirty?.(normalized !== initial);
  };

  const handlePickerChange = (v: string) => {
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
        <div
          className="h-9 w-9 shrink-0 rounded-lg border border-border"
          style={{ backgroundColor: local }}
        />
        <input
          type="color"
          value={local}
          disabled={disabled || saving}
          onChange={(e) => handlePickerChange(e.target.value)}
          className="h-9 w-9 shrink-0 cursor-pointer rounded border-0 bg-transparent p-0"
          aria-label="Color picker"
        />
        <Input
          id={definition.key}
          type="text"
          value={local}
          maxLength={7}
          disabled={disabled || saving}
          aria-invalid={!!error}
          aria-describedby={error ? `${definition.key}-error` : undefined}
          onChange={(e) => handleHexChange(e.target.value)}
          onBlur={() => {
            const err = validate();
            if (err) setError(err);
          }}
          onKeyDown={(e) => {
            if (e.key === 'Enter' && hasChanged) void handleSave();
          }}
          className="w-32 font-mono"
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
