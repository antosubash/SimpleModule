import { Button, Textarea } from '@simplemodule/ui';
import { useState } from 'react';
import { useSavedFlash, useSyncedLocal, validateRequired } from './common';
import type { FieldProps } from './types';

export default function MultilineTextField({
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
    if (definition.required && !local.trim()) return validateRequired(local);
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
      <Textarea
        id={definition.key}
        value={local}
        rows={6}
        placeholder={definition.placeholder}
        disabled={disabled || saving}
        autoFocus={autoFocus}
        aria-invalid={!!error}
        aria-describedby={error ? `${definition.key}-error` : undefined}
        onChange={(e) => handleChange(e.target.value)}
        onKeyDown={(e) => {
          if (e.key === 'Enter' && (e.ctrlKey || e.metaKey) && hasChanged) void handleSave();
        }}
      />
      <div className="flex items-center justify-between">
        <span className="text-xs text-muted-foreground">{local.length} chars</span>
        <div className="flex items-center gap-2">
          {saved && <span className="text-sm text-green-600">&#10003;</span>}
          {hasChanged && (
            <Button size="sm" onClick={() => void handleSave()} disabled={disabled || saving}>
              {saving ? 'Saving...' : 'Save'}
            </Button>
          )}
        </div>
      </div>
      {error && (
        <p id={`${definition.key}-error`} className="text-sm text-destructive">
          {error}
        </p>
      )}
    </div>
  );
}
