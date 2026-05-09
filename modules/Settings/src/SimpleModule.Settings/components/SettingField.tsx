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

const SettingTypes = {
  Text: 0,
  Number: 1,
  Bool: 2,
  Json: 3,
} as const;

function decodeForDisplay(stored: string, type: number): string {
  if (stored === '') return '';
  try {
    const parsed = JSON.parse(stored);
    switch (type) {
      case SettingTypes.Text:
        return typeof parsed === 'string' ? parsed : stored;
      case SettingTypes.Number:
        return typeof parsed === 'number' ? String(parsed) : stored;
      case SettingTypes.Bool:
        return typeof parsed === 'boolean' ? String(parsed) : stored;
      case SettingTypes.Json:
        return JSON.stringify(parsed, null, 2);
      default:
        return stored;
    }
  } catch {
    return stored;
  }
}

function encodeForStorage(input: string, type: number): string {
  switch (type) {
    case SettingTypes.Text:
      return JSON.stringify(input);
    case SettingTypes.Number: {
      const num = Number(input);
      return Number.isFinite(num) && input.trim() !== '' ? String(num) : JSON.stringify(input);
    }
    case SettingTypes.Bool:
      return input === 'true' ? 'true' : 'false';
    case SettingTypes.Json:
      return input;
    default:
      return input;
  }
}

export default function SettingField({ definition, currentValue, onSave }: SettingFieldProps) {
  const storedRaw = currentValue ?? definition.defaultValue ?? '';
  const initial = decodeForDisplay(storedRaw, definition.type);
  const [value, setValue] = useState(initial);
  const [saving, setSaving] = useState(false);
  const [jsonError, setJsonError] = useState<string | null>(null);
  const hasChanged = value !== initial;

  const handleSave = async () => {
    if (definition.type === SettingTypes.Json) {
      try {
        JSON.parse(value);
        setJsonError(null);
      } catch (err) {
        setJsonError(err instanceof Error ? err.message : 'Invalid JSON');
        return;
      }
    }
    setSaving(true);
    try {
      await onSave(definition.key, encodeForStorage(value, definition.type), definition.scope);
    } finally {
      setSaving(false);
    }
  };

  const renderInput = () => {
    switch (definition.type) {
      case SettingTypes.Text:
        return (
          <Input id={definition.key} value={value} onChange={(e) => setValue(e.target.value)} />
        );
      case SettingTypes.Number:
        return (
          <Input
            id={definition.key}
            type="number"
            value={value}
            onChange={(e) => setValue(e.target.value)}
          />
        );
      case SettingTypes.Bool:
        return (
          <Switch
            id={definition.key}
            checked={value === 'true'}
            disabled={saving}
            onCheckedChange={(checked) => {
              const newVal = String(checked);
              setValue(newVal);
              setSaving(true);
              onSave(
                definition.key,
                encodeForStorage(newVal, definition.type),
                definition.scope,
              ).finally(() => setSaving(false));
            }}
          />
        );
      case SettingTypes.Json:
        return (
          <Textarea
            id={definition.key}
            value={value}
            onChange={(e) => {
              setValue(e.target.value);
              if (jsonError) setJsonError(null);
            }}
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
      {jsonError && <p className="text-sm text-destructive">{jsonError}</p>}
      {definition.type !== SettingTypes.Bool && hasChanged && (
        <Button size="sm" onClick={handleSave} disabled={saving}>
          {saving ? 'Saving...' : 'Save'}
        </Button>
      )}
    </div>
  );
}

export type { SettingDefinition };
