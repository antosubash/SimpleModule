import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Container,
} from '@simplemodule/ui';
import { useState } from 'react';
import type { SettingDefinition } from '../components/SettingField';
import SettingField from '../components/SettingField';
import { SettingsKeys } from '../Locales/keys';

interface UserSettingView {
  definition: SettingDefinition;
  value: string | null;
  isOverridden: boolean;
}

interface UserSettingsProps {
  settings: UserSettingView[];
}

export default function UserSettings({ settings: initial }: UserSettingsProps) {
  const { t } = useTranslation('Settings');
  const [settings, setSettings] = useState(initial);

  const handleSave = async (key: string, value: string) => {
    await fetch('/api/settings/me', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ key, value, scope: 2 }),
    });
    setSettings((prev) =>
      prev.map((s) => (s.definition.key === key ? { ...s, value, isOverridden: true } : s)),
    );
  };

  const handleReset = async (key: string) => {
    await fetch(`/api/settings/me/${key}`, { method: 'DELETE' });
    setSettings((prev) =>
      prev.map((s) =>
        s.definition.key === key
          ? { ...s, isOverridden: false, value: s.definition.defaultValue ?? null }
          : s,
      ),
    );
  };

  const groups: Record<string, UserSettingView[]> = {};
  for (const s of settings) {
    const group = s.definition.group ?? 'General';
    if (!groups[group]) groups[group] = [];
    groups[group].push(s);
  }

  return (
    <Container className="space-y-6">
      <h1 className="text-2xl font-bold tracking-tight">{t(SettingsKeys.UserSettings.Title)}</h1>
      {Object.entries(groups).map(([group, items]) => (
        <Card key={group} data-testid="setting-card">
          <CardHeader>
            <CardTitle>{group}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-6">
            {items.map((s) => (
              <div key={s.definition.key} className="space-y-1">
                <div className="flex items-center justify-between">
                  <label htmlFor={s.definition.key} className="text-sm font-medium">
                    {s.definition.displayName}
                  </label>
                  <div className="flex items-center gap-2">
                    {s.isOverridden ? (
                      <>
                        <Badge variant="default">
                          {t(SettingsKeys.UserSettings.BadgeOverridden)}
                        </Badge>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleReset(s.definition.key)}
                        >
                          {t(SettingsKeys.UserSettings.ResetButton)}
                        </Button>
                      </>
                    ) : (
                      <Badge variant="info">{t(SettingsKeys.UserSettings.BadgeDefault)}</Badge>
                    )}
                  </div>
                </div>
                <SettingField
                  definition={s.definition}
                  currentValue={s.value}
                  onSave={handleSave}
                />
              </div>
            ))}
          </CardContent>
        </Card>
      ))}
    </Container>
  );
}
