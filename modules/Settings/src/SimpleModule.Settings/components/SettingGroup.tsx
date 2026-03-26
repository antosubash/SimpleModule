import { Card, CardContent, CardHeader, CardTitle } from '@simplemodule/ui';
import type { SettingDefinition } from './SettingField';
import SettingField from './SettingField';

interface SettingGroupProps {
  group: string;
  definitions: SettingDefinition[];
  values: Record<string, string | null>;
  onSave: (key: string, value: string, scope: number) => Promise<void>;
}

export default function SettingGroup({ group, definitions, values, onSave }: SettingGroupProps) {
  return (
    <Card data-testid="setting-card">
      <CardHeader>
        <CardTitle>{group}</CardTitle>
      </CardHeader>
      <CardContent className="space-y-6">
        {definitions.map((def) => (
          <div key={def.key}>
            <label htmlFor={def.key} className="text-sm font-medium">
              {def.displayName}
            </label>
            <SettingField definition={def} currentValue={values[def.key]} onSave={onSave} />
          </div>
        ))}
      </CardContent>
    </Card>
  );
}
