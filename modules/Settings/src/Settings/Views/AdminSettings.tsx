import { PageShell, Tabs, TabsContent, TabsList, TabsTrigger } from '@simplemodule/ui';
import { useMemo, useState } from 'react';
import type { SettingDefinition } from '../components/SettingField';
import SettingGroup from '../components/SettingGroup';

interface StoredSetting {
  key: string;
  value: string | null;
  scope: number;
}

interface AdminSettingsProps {
  definitions: SettingDefinition[];
  settings: StoredSetting[];
}

export default function AdminSettings({ definitions, settings }: AdminSettingsProps) {
  const [settingsMap, setSettingsMap] = useState<Record<string, string | null>>(() => {
    const map: Record<string, string | null> = {};
    for (const s of settings) {
      map[s.key] = s.value;
    }
    return map;
  });

  const systemDefs = useMemo(() => definitions.filter((d) => d.scope === 0), [definitions]);
  const appDefs = useMemo(() => definitions.filter((d) => d.scope === 1), [definitions]);

  const groupBy = (defs: SettingDefinition[]) => {
    const groups: Record<string, SettingDefinition[]> = {};
    for (const def of defs) {
      const group = def.group ?? 'General';
      if (!groups[group]) groups[group] = [];
      groups[group].push(def);
    }
    return groups;
  };

  const handleSave = async (key: string, value: string, scope: number) => {
    await fetch('/api/settings', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ key, value, scope }),
    });
    setSettingsMap((prev) => ({ ...prev, [key]: value }));
  };

  return (
    <PageShell title="Settings">
      <Tabs defaultValue="system">
        <TabsList>
          <TabsTrigger value="system">System</TabsTrigger>
          <TabsTrigger value="application">Application</TabsTrigger>
        </TabsList>
        <TabsContent value="system" className="space-y-4">
          {Object.entries(groupBy(systemDefs)).map(([group, defs]) => (
            <SettingGroup
              key={group}
              group={group}
              definitions={defs}
              values={settingsMap}
              onSave={handleSave}
            />
          ))}
        </TabsContent>
        <TabsContent value="application" className="space-y-4">
          {Object.entries(groupBy(appDefs)).map(([group, defs]) => (
            <SettingGroup
              key={group}
              group={group}
              definitions={defs}
              values={settingsMap}
              onSave={handleSave}
            />
          ))}
        </TabsContent>
      </Tabs>
    </PageShell>
  );
}
