import { useTranslation } from '@simplemodule/client/use-translation';
import { Badge, Button, Label } from '@simplemodule/ui';
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

const scopeBadgeVariant: Record<number, 'danger' | 'info' | 'success'> = {
  0: 'danger',
  1: 'info',
  2: 'success',
};

// Wide types (Json, MultilineText) need their own row below the label.
const WIDE_TYPES = new Set([3, 9]);

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

  const scopeVariant = scopeBadgeVariant[definition.scope] ?? 'default';
  const isWide = WIDE_TYPES.has(definition.type);

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

  const labelBlock = (
    <div className="min-w-0 flex-1">
      <div className="flex flex-wrap items-center gap-2 mb-1">
        <Label htmlFor={definition.key} className="text-sm font-semibold text-text">
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
        </Label>
        <Badge variant={scopeVariant}>{scopeLabel}</Badge>
        {valueInfo.isOverridden && <Badge variant="warning">{t(keys.Overridden)}</Badge>}
        {!valueInfo.isOverridden && namespace === 'UserSettings' && (
          <Badge variant="default">{t(keys.Default)}</Badge>
        )}
      </div>
      {definition.description && (
        <p className="text-sm text-text-secondary max-w-prose">{definition.description}</p>
      )}
      {showResolvedValue && resolvedDisplay !== null && (
        <p className="text-xs text-text-muted mt-1.5">
          {t(keys.Current)}:{' '}
          <span className="font-mono text-text-secondary">{resolvedDisplay}</span>{' '}
          <span className="text-text-muted">
            &#8592; ({valueInfo.isOverridden ? t(keys.YourOverride) : t(keys.InheritedDefault)})
          </span>
        </p>
      )}
    </div>
  );

  const fieldBlock = (
    <SettingField
      definition={definition}
      value={valueInfo.value}
      onSave={handleSave}
      onDirty={onDirty ? (isDirty: boolean) => onDirty(definition.key, isDirty) : undefined}
      disabled={disabled}
    />
  );

  const resetButton = !bulkMode && handleReset && (
    <Button
      variant="ghost"
      size="sm"
      onClick={handleReset}
      className="text-text-secondary hover:text-danger-text whitespace-nowrap shrink-0"
    >
      {t(keys.ResetToDefault)}
    </Button>
  );

  if (isWide) {
    return (
      <div className="px-6 py-5">
        <div className="flex items-start justify-between gap-4 mb-3">
          {labelBlock}
          {resetButton}
        </div>
        {fieldBlock}
      </div>
    );
  }

  return (
    <div className="px-6 py-5">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:gap-6">
        {labelBlock}
        <div className="flex items-center gap-3 shrink-0 sm:w-72">
          <div className="flex-1 min-w-0">{fieldBlock}</div>
          {resetButton}
        </div>
      </div>
    </div>
  );
}
