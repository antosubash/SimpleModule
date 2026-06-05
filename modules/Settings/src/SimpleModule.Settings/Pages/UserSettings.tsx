import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Alert,
  AlertDescription,
  AlertTitle,
  Button,
  EmptyState,
  PageShell,
} from '@simplemodule/ui';
import { useCallback, useMemo, useState } from 'react';
import type { SettingDefinition } from '@/components/SettingField';
import SettingGroup from '@/components/SettingGroup';
import SettingRow from '@/components/SettingRow';
import { toAnchorId } from '@/components/SettingsGroupNav';
import SettingsSearch from '@/components/SettingsSearch';
import { SettingsKeys } from '@/Locales/keys';

interface UserSettingValueDto {
  key: string;
  value: unknown | null;
  resolvedValue: unknown | null;
  isOverridden: boolean;
}

interface ValidationProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
}

interface UserSettingsProps {
  definitions: SettingDefinition[];
  settings: UserSettingValueDto[];
}

function buildValueMap(settings: UserSettingValueDto[]): Map<string, UserSettingValueDto> {
  const map = new Map<string, UserSettingValueDto>();
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

async function parseErrorDetail(res: Response): Promise<string> {
  try {
    const body = (await res.json()) as ValidationProblemDetails;
    if (body.detail) return body.detail;
    if (body.title) return body.title;
    if (body.errors) {
      const messages = Object.values(body.errors).flat();
      if (messages.length > 0) return messages.join(' ');
    }
  } catch {
    // not JSON — fall through to generic message
  }
  return `HTTP ${res.status}`;
}

export default function UserSettings({ definitions, settings }: UserSettingsProps) {
  const { t } = useTranslation('Settings');

  const [valueMap, setValueMap] = useState<Map<string, UserSettingValueDto>>(() =>
    buildValueMap(settings),
  );

  const [query, setQuery] = useState('');
  const [onlyOverridden, setOnlyOverridden] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const filteredDefs = useMemo(() => {
    return definitions.filter((def) => {
      if (!matchesSearch(def, query)) return false;
      if (onlyOverridden) {
        const v = valueMap.get(def.key);
        return v?.isOverridden ?? false;
      }
      return true;
    });
  }, [definitions, query, onlyOverridden, valueMap]);

  const grouped = useMemo(() => groupDefinitions(filteredDefs), [filteredDefs]);
  const groupNames = useMemo(() => Object.keys(grouped), [grouped]);

  const handleSave = useCallback(async (key: string, _scope: number, value: unknown) => {
    setErrorMessage(null);
    const res = await fetch('/api/settings/me', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ key, value, scope: 2 }),
    });
    if (!res.ok) {
      const detail = await parseErrorDetail(res);
      setErrorMessage(detail);
      return;
    }
    setValueMap((prev) => {
      const next = new Map(prev);
      const existing = next.get(key);
      if (existing) {
        next.set(key, { ...existing, value, isOverridden: true, resolvedValue: value });
      }
      return next;
    });
  }, []);

  const handleReset = useCallback(async (key: string, _scope: number) => {
    setErrorMessage(null);
    const res = await fetch(`/api/settings/me/${encodeURIComponent(key)}`, { method: 'DELETE' });
    if (!res.ok) {
      const detail = await parseErrorDetail(res);
      setErrorMessage(detail);
      return;
    }
    setValueMap((prev) => {
      const next = new Map(prev);
      const existing = next.get(key);
      if (existing) {
        next.set(key, {
          ...existing,
          value: null,
          isOverridden: false,
          resolvedValue: existing.resolvedValue,
        });
      }
      return next;
    });
  }, []);

  return (
    <PageShell title={t(SettingsKeys.UserSettings.Title)} size="lg">
      <div
        className="sticky z-30 -mx-4 bg-surface/95 backdrop-blur-sm border-b border-border px-4 py-3 mb-6 sm:-mx-6 sm:px-6 lg:-mx-8 lg:px-8"
        style={{ top: 0 }}
      >
        <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:gap-3">
          <SettingsSearch
            query={query}
            onQueryChange={(q) => {
              setQuery(q);
              setErrorMessage(null);
            }}
            showOnlyModified={onlyOverridden}
            onShowOnlyModifiedChange={setOnlyOverridden}
            modifiedLabel={t(SettingsKeys.UserSettings.OnlyOverridden)}
          />
        </div>
      </div>

      {errorMessage !== null && (
        <Alert variant="danger" className="mb-4">
          <AlertTitle>{t(SettingsKeys.UserSettings.SaveErrorTitle)}</AlertTitle>
          <AlertDescription>{errorMessage}</AlertDescription>
          <Button
            variant="ghost"
            size="sm"
            className="mt-2 h-auto px-2 py-1 text-xs"
            onClick={() => setErrorMessage(null)}
          >
            Dismiss
          </Button>
        </Alert>
      )}

      <div className="flex gap-8">
        <aside className="hidden lg:block w-48 flex-shrink-0">
          <nav aria-label="Settings groups" className="sticky top-28 space-y-0.5">
            {groupNames.map((group) => (
              <a
                key={group}
                href={`#${toAnchorId(group)}`}
                onClick={(e) => {
                  e.preventDefault();
                  document
                    .getElementById(toAnchorId(group))
                    ?.scrollIntoView({ behavior: 'smooth', block: 'start' });
                }}
                className="block rounded-lg px-3 py-2 text-sm text-text-secondary hover:bg-surface-raised hover:text-text transition-colors duration-150"
              >
                {group}
              </a>
            ))}
          </nav>
        </aside>

        <div className="flex-1 min-w-0 pb-8">
          {groupNames.length === 0 ? (
            <EmptyState
              title={t(SettingsKeys.UserSettings.NoResults)}
              description={
                query ? `No settings match "${query}". Try a different search term.` : undefined
              }
              secondaryAction={
                query ? (
                  <Button variant="outline" size="sm" onClick={() => setQuery('')}>
                    Clear search
                  </Button>
                ) : undefined
              }
            />
          ) : (
            <div className="space-y-8">
              {groupNames.map((group) => (
                <SettingGroup key={group} group={group}>
                  {(grouped[group] ?? []).map((def) => {
                    const v = valueMap.get(def.key);
                    return (
                      <SettingRow
                        key={def.key}
                        definition={def}
                        valueInfo={{
                          value: v?.value ?? null,
                          isOverridden: v?.isOverridden ?? false,
                          resolvedValue: v?.resolvedValue ?? null,
                        }}
                        onSave={handleSave}
                        onReset={handleReset}
                        showResolvedValue
                        namespace="UserSettings"
                      />
                    );
                  })}
                </SettingGroup>
              ))}
            </div>
          )}
        </div>
      </div>
    </PageShell>
  );
}
