import { useTranslation } from '@simplemodule/client/use-translation';
import { Checkbox, Label, SearchInput } from '@simplemodule/ui';
import { useId } from 'react';
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
  const checkboxId = useId();

  return (
    <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:gap-4">
      <SearchInput
        aria-label={t(SettingsKeys.AdminSettings.SearchPlaceholder)}
        placeholder={t(SettingsKeys.AdminSettings.SearchPlaceholder)}
        value={query}
        onChange={(e) => onQueryChange(e.target.value)}
        className="sm:max-w-xs"
      />
      <div className="flex items-center gap-2">
        <Checkbox
          id={checkboxId}
          checked={showOnlyModified}
          onCheckedChange={(v) => onShowOnlyModifiedChange(v === true)}
        />
        <Label
          htmlFor={checkboxId}
          className="text-sm text-text-secondary cursor-pointer whitespace-nowrap"
        >
          {modifiedLabel}
        </Label>
      </div>
    </div>
  );
}
