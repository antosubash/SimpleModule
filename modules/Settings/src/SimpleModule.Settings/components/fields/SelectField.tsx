import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@simplemodule/ui';
import { useState } from 'react';
import { useSyncedLocal } from './common';
import type { FieldProps } from './types';

export default function SelectField({ definition, value, onSave, onDirty, disabled }: FieldProps) {
  const initial = typeof value === 'string' ? value : '';
  const [local, setLocal] = useSyncedLocal(initial);
  const [saving, setSaving] = useState(false);

  const options = definition.allowedValues ?? [];

  const handleChange = async (next: string) => {
    setLocal(next);
    onDirty?.(next !== initial);
    setSaving(true);
    try {
      await onSave(next);
      onDirty?.(false);
    } finally {
      setSaving(false);
    }
  };

  return (
    <Select value={local} disabled={disabled || saving} onValueChange={(v) => void handleChange(v)}>
      <SelectTrigger id={definition.key} aria-describedby={undefined}>
        <SelectValue placeholder={definition.placeholder ?? 'Select an option'} />
      </SelectTrigger>
      <SelectContent>
        {options.map((opt) => (
          <SelectItem key={opt} value={opt}>
            {opt}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}
