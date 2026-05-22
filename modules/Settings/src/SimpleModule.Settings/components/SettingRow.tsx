import { useTranslation } from '@simplemodule/client/use-translation';
import { Badge, Button } from '@simplemodule/ui';
import { SettingsKeys } from '@/Locales/keys';
import type { SettingDefinition } from './SettingField';
import SettingField from './SettingField';

interface SettingValueInfo {
  value: unknown | null;
  isOverridden: boolean;
  resolvedValue?: unknown | null;
}

interface SettingRowProps {
  definition: SettingDefinition;
  valueInfo: SettingValueInfo;
  onSave: (key: string, scope: number, value: unknown) => Promise<void>;
  onReset?: (key: string, scope: number) => Promise<void>;
  onDirty?: (key: string, isDirty: boolean) => void;
  showResolvedValue?: boolean;
  bulkMode?: boolean;
  disabled?: boolean;
  namespace: 'AdminSettings' | 'UserSettings';
}

const scopeColorMap: Record<number, string> = {
  0: 'bg-danger-bg text-danger-text',
  1: 'bg-info-bg text-primary',
  2: 'bg-success-bg text-success-text',
};

function formatResolved(value: unknown): string | null {
  if (value === null || value === undefined) return null;
  if (typeof value === 'string') return value;
  return JSON.stringify(value);
}

export default function SettingRow({
  definition,
  valueInfo,
  onSave,
  onReset,
  onDirty,
  showResolvedValue = false,
  bulkMode = false,
  disabled = false,
  namespace,
}: SettingRowProps) {
  const { t } = useTranslation('Settings');
  const keys = SettingsKeys[namespace];

  const scopeClass = scopeColorMap[definition.scope] ?? 'bg-surface-raised text-text-secondary';

  const scopeLabel = (() => {
    switch (definition.scope) {
      case 0:
        return t(SettingsKeys.AdminSettings.ScopeSystem);
      case 1:
        return t(SettingsKeys.AdminSettings.ScopeApplication);
      case 2:
        return t(SettingsKeys.AdminSettings.ScopeUser);
      default:
        return String(definition.scope);
    }
  })();

  const resolvedDisplay = showResolvedValue
    ? (formatResolved(valueInfo.resolvedValue) ?? formatResolved(definition.defaultValue))
    : null;

  const handleSave = async (value: unknown) => {
    await onSave(definition.key, definition.scope, value);
  };

  const handleReset =
    onReset && valueInfo.isOverridden
      ? async () => {
          await onReset(definition.key, definition.scope);
        }
      : undefined;

  return (
    <div className="py-5 first:pt-0 last:pb-0">
      <div className="flex flex-wrap items-start justify-between gap-3 mb-1.5">
        <div className="flex flex-wrap items-center gap-2 min-w-0">
          <label htmlFor={definition.key} className="text-sm font-semibold text-text">
            {definition.displayName}
            {definition.required && (
              <span
                className="ml-0.5 text-danger-text"
                title={t(SettingsKeys.AdminSettings.RequiredAsterisk)}
                aria-hidden="true"
              >
                *
              </span>
            )}
          </label>
          <span
            className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${scopeClass}`}
          >
            {scopeLabel}
          </span>
          {valueInfo.isOverridden && <Badge variant="warning">{t(keys.Overridden)}</Badge>}
          {!valueInfo.isOverridden && namespace === 'UserSettings' && (
            <Badge variant="default">{t(keys.Default)}</Badge>
          )}
        </div>

        {!bulkMode && handleReset && (
          <Button
            variant="ghost"
            size="sm"
            onClick={handleReset}
            className="text-text-secondary hover:text-danger-text whitespace-nowrap -my-1"
          >
            {t(keys.ResetToDefault)}
          </Button>
        )}
      </div>

      {definition.description && (
        <p className="text-sm text-text-secondary mb-3 max-w-prose">{definition.description}</p>
      )}

      <div className="max-w-xl">
        <SettingField
          definition={definition}
          value={valueInfo.value}
          onSave={handleSave}
          onDirty={onDirty ? (isDirty: boolean) => onDirty(definition.key, isDirty) : undefined}
          disabled={disabled}
        />
      </div>

      {showResolvedValue && resolvedDisplay !== null && (
        <p className="text-xs text-text-muted mt-2">
          {t(keys.Current)}:{' '}
          <span className="font-mono text-text-secondary">{resolvedDisplay}</span>{' '}
          <span className="text-text-muted">
            &#8592; ({valueInfo.isOverridden ? t(keys.YourOverride) : t(keys.InheritedDefault)})
          </span>
        </p>
      )}
    </div>
  );
}
