import { useTranslation } from '@simplemodule/client/use-translation';
import { PageShell, Tabs, TabsContent, TabsList, TabsTrigger, Toggle } from '@simplemodule/ui';
import { useCallback, useMemo, useState } from 'react';
import type { SettingDefinition } from '@/components/SettingField';
import SettingGroup from '@/components/SettingGroup';
import SettingRow from '@/components/SettingRow';
import SettingsBulkSaveBar from '@/components/SettingsBulkSaveBar';
import SettingsLayout from '@/components/SettingsLayout';
import SettingsSearch from '@/components/SettingsSearch';
import { SettingsKeys } from '@/Locales/keys';

interface SettingValueDto {
  key: string;
  scope: 0 | 1 | 2;
  value: unknown | null;
  isOverridden: boolean;
  userId?: string | null;
  updatedAt?: string | null;
}

interface AdminSettingsProps {
  definitions: SettingDefinition[];
  settings: SettingValueDto[];
}

function buildValueMap(settings: SettingValueDto[]): Map<string, SettingValueDto> {
  const map = new Map<string, SettingValueDto>();
  for (const s of settings) {
    map.set(s.key, s);
  }
  return map;
}

function groupDefinitions(defs: SettingDefinition[]): Record<string, SettingDefinition[]> {
  const groups: Record<string, SettingDefinition[]> = {};
  const sorted = [...defs].sort((a, b) => a.order - b.order);
  for (const def of sorted) {
    const group = def.group ?? 'General';
    if (!groups[group]) groups[group] = [];
    groups[group].push(def);
  }
  return groups;
}

function matchesSearch(def: SettingDefinition, query: string): boolean {
  if (!query) return true;
  const q = query.toLowerCase();
  return (
    def.displayName.toLowerCase().includes(q) ||
    def.key.toLowerCase().includes(q) ||
    (def.group ?? '').toLowerCase().includes(q) ||
    (def.description ?? '').toLowerCase().includes(q)
  );
}

export default function AdminSettings({ definitions, settings }: AdminSettingsProps) {
  const { t } = useTranslation('Settings');

  const [valueMap, setValueMap] = useState<Map<string, SettingValueDto>>(() =>
    buildValueMap(settings),
  );

  const [query, setQuery] = useState('');
  const [showOnlyModified, setShowOnlyModified] = useState(false);
  const [bulkMode, setBulkMode] = useState(false);
  const [dirtyKeys, setDirtyKeys] = useState<Set<string>>(new Set());
  const [pendingValues, setPendingValues] = useState<
    Map<string, { scope: number; value: unknown }>
  >(new Map());
  const [bulkSaving, setBulkSaving] = useState(false);

  const systemDefs = useMemo(() => definitions.filter((d) => d.scope === 0), [definitions]);
  const appDefs = useMemo(() => definitions.filter((d) => d.scope === 1), [definitions]);

  const filterDefs = useCallback(
    (defs: SettingDefinition[]): SettingDefinition[] => {
      return defs.filter((def) => {
        if (!matchesSearch(def, query)) return false;
        if (showOnlyModified) {
          const v = valueMap.get(def.key);
          return v?.isOverridden ?? false;
        }
        return true;
      });
    },
    [query, showOnlyModified, valueMap],
  );

  const handleSave = async (key: string, scope: number, value: unknown) => {
    if (bulkMode) {
      setPendingValues((prev) => new Map(prev).set(key, { scope, value }));
      return;
    }
    await fetch('/api/settings', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ key, scope, value }),
    });
    setValueMap((prev) => {
      const next = new Map(prev);
      const existing = next.get(key);
      next.set(key, { ...existing, key, scope: scope as 0 | 1 | 2, value, isOverridden: true });
      return next;
    });
  };

  const handleReset = async (key: string, scope: number) => {
    await fetch(`/api/settings/${encodeURIComponent(key)}?scope=${scope}`, {
      method: 'DELETE',
    });
    setValueMap((prev) => {
      const next = new Map(prev);
      const existing = next.get(key);
      if (existing) {
        next.set(key, { ...existing, value: null, isOverridden: false });
      }
      return next;
    });
    setDirtyKeys((prev) => {
      const next = new Set(prev);
      next.delete(key);
      return next;
    });
    setPendingValues((prev) => {
      const next = new Map(prev);
      next.delete(key);
      return next;
    });
  };

  const handleDirty = useCallback((key: string, isDirty: boolean) => {
    setDirtyKeys((prev) => {
      const next = new Set(prev);
      if (isDirty) next.add(key);
      else next.delete(key);
      return next;
    });
  }, []);

  const handleBulkSave = async () => {
    if (pendingValues.size === 0) return;
    setBulkSaving(true);
    try {
      const updates = Array.from(pendingValues.entries()).map(([key, { scope, value }]) => ({
        key,
        scope,
        value,
      }));
      await fetch('/api/settings/bulk', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ updates }),
      });
      setValueMap((prev) => {
        const next = new Map(prev);
        for (const { key, scope, value } of updates) {
          const existing = next.get(key);
          next.set(key, {
            ...existing,
            key,
            scope: scope as 0 | 1 | 2,
            value,
            isOverridden: true,
          });
        }
        return next;
      });
      setPendingValues(new Map());
      setDirtyKeys(new Set());
    } finally {
      setBulkSaving(false);
    }
  };

  const handleDiscard = () => {
    setPendingValues(new Map());
    setDirtyKeys(new Set());
  };

  const renderGroups = (defs: SettingDefinition[]) => {
    const filtered = filterDefs(defs);
    const grouped = groupDefinitions(filtered);
    const groupNames = Object.keys(grouped);

    if (groupNames.length === 0) {
      return (
        <p className="py-12 text-center text-sm text-text-muted">No settings match your search.</p>
      );
    }

    return (
      <SettingsLayout
        groups={groupNames}
        toolbar={
          <div className="flex flex-wrap items-center justify-between gap-3">
            <SettingsSearch
              query={query}
              onQueryChange={setQuery}
              showOnlyModified={showOnlyModified}
              onShowOnlyModifiedChange={setShowOnlyModified}
              modifiedLabel={t(SettingsKeys.AdminSettings.ShowOnlyModified)}
            />
            <Toggle
              pressed={bulkMode}
              onPressedChange={(v) => {
                setBulkMode(v);
                if (!v) handleDiscard();
              }}
              variant="outline"
              aria-label={t(SettingsKeys.AdminSettings.BulkEditToggle)}
            >
              {t(SettingsKeys.AdminSettings.BulkEditToggle)}
            </Toggle>
          </div>
        }
      >
        {groupNames.map((group) => (
          <SettingGroup key={group} group={group}>
            {(grouped[group] ?? []).map((def) => {
              const v = valueMap.get(def.key);
              const pending = pendingValues.get(def.key);
              return (
                <SettingRow
                  key={def.key}
                  definition={def}
                  valueInfo={{
                    value: pending?.value ?? v?.value ?? null,
                    isOverridden: v?.isOverridden ?? false,
                  }}
                  onSave={handleSave}
                  onReset={handleReset}
                  onDirty={handleDirty}
                  bulkMode={bulkMode}
                  namespace="AdminSettings"
                />
              );
            })}
          </SettingGroup>
        ))}
      </SettingsLayout>
    );
  };

  const totalDirty = bulkMode ? pendingValues.size : dirtyKeys.size;

  return (
    <PageShell title={t(SettingsKeys.AdminSettings.Title)} size="lg">
      <Tabs defaultValue="system" className="mt-2">
        <TabsList className="w-full sm:w-auto">
          <TabsTrigger value="system">{t(SettingsKeys.AdminSettings.TabSystem)}</TabsTrigger>
          <TabsTrigger value="application">
            {t(SettingsKeys.AdminSettings.TabApplication)}
          </TabsTrigger>
        </TabsList>
        <TabsContent value="system" className="mt-4">
          {renderGroups(systemDefs)}
        </TabsContent>
        <TabsContent value="application" className="mt-4">
          {renderGroups(appDefs)}
        </TabsContent>
      </Tabs>

      {bulkMode && (
        <SettingsBulkSaveBar
          dirtyCount={totalDirty}
          onSaveAll={handleBulkSave}
          onDiscard={handleDiscard}
          saving={bulkSaving}
        />
      )}
    </PageShell>
  );
}
