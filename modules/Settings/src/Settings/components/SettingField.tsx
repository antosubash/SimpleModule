import { Button, Input, Switch, Textarea } from '@simplemodule/ui';
import { useState } from 'react';

interface SettingDefinition {
  key: string;
  displayName: string;
  description?: string;
  group?: string;
  scope: number;
  defaultValue?: string;
  type: number;
}

interface SettingFieldProps {
  definition: SettingDefinition;
  currentValue?: string | null;
  onSave: (key: string, value: string, scope: number) => Promise<void>;
}

export default function SettingField({ definition, currentValue, onSave }: SettingFieldProps) {
  const initial = currentValue ?? definition.defaultValue ?? '';
  const [value, setValue] = useState(initial);
  const [saving, setSaving] = useState(false);
  const hasChanged = value !== initial;

  const handleSave = async () => {
    setSaving(true);
    try {
      await onSave(definition.key, value, definition.scope);
    } finally {
      setSaving(false);
    }
  };

  const renderInput = () => {
    switch (definition.type) {
      case 0: // Text
        return <Input value={value} onChange={(e) => setValue(e.target.value)} />;
      case 1: // Number
        return <Input type="number" value={value} onChange={(e) => setValue(e.target.value)} />;
      case 2: // Bool
        return (
          <Switch
            checked={value === 'true'}
            onCheckedChange={(checked) => {
              const newVal = String(checked);
              setValue(newVal);
              onSave(definition.key, newVal, definition.scope);
            }}
          />
        );
      case 3: // Json
        return (
          <Textarea
            value={value}
            onChange={(e) => setValue(e.target.value)}
            rows={4}
            className="font-mono text-sm"
          />
        );
      default:
        return null;
    }
  };

  return (
    <div className="space-y-2">
      {definition.description && (
        <p className="text-sm text-muted-foreground">{definition.description}</p>
      )}
      {renderInput()}
      {definition.type !== 2 && hasChanged && (
        <Button size="sm" onClick={handleSave} disabled={saving}>
          {saving ? 'Saving...' : 'Save'}
        </Button>
      )}
    </div>
  );
}

export type { SettingDefinition };
