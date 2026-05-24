import { Switch } from '@simplemodule/ui';
import { useState } from 'react';
import { useSyncedLocal } from './common';
import type { FieldProps } from './types';

export default function BoolField({ definition, value, onSave, onDirty, disabled }: FieldProps) {
  const [checked, setChecked] = useSyncedLocal(value === true);
  const [saving, setSaving] = useState(false);

  const handleChange = async (next: boolean) => {
    setChecked(next);
    onDirty?.(next !== (value === true));
    setSaving(true);
    try {
      await onSave(next);
      onDirty?.(false);
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="flex items-center gap-3">
      <Switch
        id={definition.key}
        checked={checked}
        disabled={disabled || saving}
        onCheckedChange={(v) => void handleChange(v)}
        aria-describedby={undefined}
      />
      <span className="text-sm text-muted-foreground">{checked ? 'On' : 'Off'}</span>
    </div>
  );
}
