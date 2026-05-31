import { useTranslation } from '@simplemodule/client/use-translation';
import { Button } from '@simplemodule/ui';
import { SettingsKeys } from '@/Locales/keys';

interface SettingsBulkSaveBarProps {
  dirtyCount: number;
  onSaveAll: () => Promise<void>;
  onDiscard: () => void;
  saving: boolean;
}

export default function SettingsBulkSaveBar({
  dirtyCount,
  onSaveAll,
  onDiscard,
  saving,
}: SettingsBulkSaveBarProps) {
  const { t } = useTranslation('Settings');

  if (dirtyCount === 0) {
    return null;
  }

  const label = t(SettingsKeys.AdminSettings.UnsavedChanges).replace('{count}', String(dirtyCount));

  return (
    <div
      role="status"
      aria-live="polite"
      className="fixed bottom-0 left-0 right-0 z-50 border-t border-border bg-surface/95 backdrop-blur-sm shadow-lg"
    >
      <div className="mx-auto flex max-w-7xl items-center justify-between gap-4 px-4 py-3 sm:px-6 lg:px-8">
        <p className="text-sm text-text-secondary font-medium">{label}</p>
        <div className="flex items-center gap-2">
          <Button variant="secondary" size="sm" onClick={onDiscard} disabled={saving}>
            {t(SettingsKeys.AdminSettings.BulkDiscardButton)}
          </Button>
          <Button
            variant="primary"
            size="sm"
            onClick={() => void onSaveAll()}
            isLoading={saving}
            loadingText={t(SettingsKeys.AdminSettings.BulkSaveButton)}
          >
            {t(SettingsKeys.AdminSettings.BulkSaveButton)}
          </Button>
        </div>
      </div>
    </div>
  );
}
