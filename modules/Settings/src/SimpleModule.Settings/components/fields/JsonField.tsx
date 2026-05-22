import { Button, Textarea } from '@simplemodule/ui';
import { useState } from 'react';
import { useSavedFlash, useSyncedLocal, validateRequired } from './common';
import type { FieldProps } from './types';

function prettyPrint(raw: unknown): string {
  if (raw === null || raw === undefined) return '';
  if (typeof raw === 'string') {
    try {
      return JSON.stringify(JSON.parse(raw), null, 2);
    } catch {
      return raw;
    }
  }
  return JSON.stringify(raw, null, 2);
}

export default function JsonField({
  definition,
  value,
  onSave,
  onDirty,
  disabled,
  autoFocus,
}: FieldProps) {
  const initial = prettyPrint(value);
  const [local, setLocal] = useSyncedLocal(initial);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [saved, flash] = useSavedFlash();

  const hasChanged = local !== initial;

  const parseAndValidate = (): [unknown, string | null] => {
    if (definition.required && local.trim() === '') return [null, validateRequired(local)];
    if (local.trim() === '') return [null, null];
    try {
      return [JSON.parse(local), null];
    } catch (err) {
      return [null, err instanceof Error ? err.message : 'Invalid JSON'];
    }
  };

  const handleChange = (v: string) => {
    setLocal(v);
    if (error) setError(null);
    onDirty?.(v !== initial);
  };

  const handleFormat = () => {
    const [parsed, err] = parseAndValidate();
    if (err) {
      setError(err);
      return;
    }
    const formatted = JSON.stringify(parsed, null, 2);
    setLocal(formatted);
    onDirty?.(formatted !== initial);
  };

  const handleSave = async () => {
    const [parsed, err] = parseAndValidate();
    if (err) {
      setError(err);
      return;
    }
    setSaving(true);
    try {
      await onSave(parsed);
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
        className="font-mono text-sm"
        placeholder={definition.placeholder ?? '{}'}
        disabled={disabled || saving}
        autoFocus={autoFocus}
        aria-invalid={!!error}
        aria-describedby={error ? `${definition.key}-error` : undefined}
        onChange={(e) => handleChange(e.target.value)}
        onKeyDown={(e) => {
          if (e.key === 'Enter' && (e.ctrlKey || e.metaKey) && hasChanged) void handleSave();
        }}
      />
      {error && (
        <p id={`${definition.key}-error`} className="text-sm text-destructive">
          {error}
        </p>
      )}
      <div className="flex items-center gap-2">
        <Button size="sm" variant="outline" onClick={handleFormat} disabled={disabled || saving}>
          Format
        </Button>
        {hasChanged && (
          <Button size="sm" onClick={() => void handleSave()} disabled={disabled || saving}>
            {saving ? 'Saving...' : 'Save'}
          </Button>
        )}
        {saved && <span className="text-sm text-green-600">&#10003;</span>}
      </div>
    </div>
  );
}
