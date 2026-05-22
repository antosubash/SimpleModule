import { useTranslation } from '@simplemodule/client/use-translation';
import { SearchInput, Toggle } from '@simplemodule/ui';
import { SettingsKeys } from '@/Locales/keys';

interface SettingsSearchProps {
  query: string;
  onQueryChange: (q: string) => void;
  showOnlyModified: boolean;
  onShowOnlyModifiedChange: (v: boolean) => void;
  modifiedLabel: string;
}

export default function SettingsSearch({
  query,
  onQueryChange,
  showOnlyModified,
  onShowOnlyModifiedChange,
  modifiedLabel,
}: SettingsSearchProps) {
  const { t } = useTranslation('Settings');

  return (
    <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:gap-3">
      <SearchInput
        aria-label={t(SettingsKeys.AdminSettings.SearchPlaceholder)}
        placeholder={t(SettingsKeys.AdminSettings.SearchPlaceholder)}
        value={query}
        onChange={(e) => onQueryChange(e.target.value)}
        className="sm:max-w-xs"
      />
      <Toggle
        pressed={showOnlyModified}
        onPressedChange={onShowOnlyModifiedChange}
        variant="outline"
        size="default"
        aria-label={modifiedLabel}
      >
        {modifiedLabel}
      </Toggle>
    </div>
  );
}
