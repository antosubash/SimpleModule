import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Button,
  Card,
  CardContent,
  Input,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@simplemodule/ui';
import type { FormEvent } from 'react';
import { AuditLogsKeys } from '@/Locales/keys';
import { ACTION_LABELS, SOURCE_LABELS } from '@/utils/audit-utils';

const DATE_PRESETS = [
  { label: 'Last hour', hours: 1 },
  { label: 'Last 24h', hours: 24 },
  { label: 'Last 7 days', hours: 168 },
  { label: 'Last 30 days', hours: 720 },
];

interface Props {
  from: string;
  to: string;
  source: string;
  action: string;
  module: string;
  searchText: string;
  hasActiveFilters: boolean;
  onFromChange: (v: string) => void;
  onToChange: (v: string) => void;
  onSourceChange: (v: string) => void;
  onActionChange: (v: string) => void;
  onModuleChange: (v: string) => void;
  onSearchTextChange: (v: string) => void;
  onApplyFilters: (e?: FormEvent) => void;
  onClearFilters: () => void;
  onApplyDatePreset: (hours: number) => void;
}

export function BrowseFilters({
  from,
  to,
  source,
  action,
  module,
  searchText,
  hasActiveFilters,
  onFromChange,
  onToChange,
  onSourceChange,
  onActionChange,
  onModuleChange,
  onSearchTextChange,
  onApplyFilters,
  onClearFilters,
  onApplyDatePreset,
}: Props) {
  const { t } = useTranslation('AuditLogs');
  return (
    <Card>
      <CardContent>
        <div className="mb-3 flex flex-col gap-2 sm:flex-row sm:flex-wrap sm:items-center">
          <span className="text-xs font-medium text-text-muted">
            {t(AuditLogsKeys.Browse.QuickRange)}
          </span>
          {DATE_PRESETS.map((preset) => (
            <Button
              key={preset.hours}
              variant="ghost"
              size="sm"
              onClick={() => onApplyDatePreset(preset.hours)}
            >
              {preset.label}
            </Button>
          ))}
        </div>

        <form onSubmit={onApplyFilters}>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 sm:gap-4 md:grid-cols-3 lg:grid-cols-4">
            <div className="space-y-1">
              <label htmlFor="filter-from" className="text-xs font-medium text-text-muted">
                {t(AuditLogsKeys.Browse.FilterFrom)}
              </label>
              <Input
                id="filter-from"
                type="datetime-local"
                value={from}
                onChange={(e) => onFromChange(e.target.value)}
              />
            </div>
            <div className="space-y-1">
              <label htmlFor="filter-to" className="text-xs font-medium text-text-muted">
                {t(AuditLogsKeys.Browse.FilterTo)}
              </label>
              <Input
                id="filter-to"
                type="datetime-local"
                value={to}
                onChange={(e) => onToChange(e.target.value)}
              />
            </div>
            <div className="space-y-1">
              <span className="text-xs font-medium text-text-muted">
                {t(AuditLogsKeys.Browse.FilterSource)}
              </span>
              <Select value={source} onValueChange={onSourceChange}>
                <SelectTrigger aria-label={t(AuditLogsKeys.Browse.FilterSource)}>
                  <SelectValue placeholder={t(AuditLogsKeys.Browse.FilterSourcePlaceholder)} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="__all__">{t(AuditLogsKeys.Browse.FilterSourceAll)}</SelectItem>
                  {Object.entries(SOURCE_LABELS).map(([k, v]) => (
                    <SelectItem key={k} value={k}>
                      {v}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1">
              <span className="text-xs font-medium text-text-muted">
                {t(AuditLogsKeys.Browse.FilterAction)}
              </span>
              <Select value={action} onValueChange={onActionChange}>
                <SelectTrigger aria-label={t(AuditLogsKeys.Browse.FilterAction)}>
                  <SelectValue placeholder={t(AuditLogsKeys.Browse.FilterActionPlaceholder)} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="__all__">{t(AuditLogsKeys.Browse.FilterActionAll)}</SelectItem>
                  {Object.entries(ACTION_LABELS).map(([k, v]) => (
                    <SelectItem key={k} value={k}>
                      {v}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1">
              <label htmlFor="filter-module" className="text-xs font-medium text-text-muted">
                {t(AuditLogsKeys.Browse.FilterModule)}
              </label>
              <Input
                id="filter-module"
                placeholder={t(AuditLogsKeys.Browse.FilterModulePlaceholder)}
                value={module}
                onChange={(e) => onModuleChange(e.target.value)}
              />
            </div>
            <div className="space-y-1">
              <label htmlFor="filter-search" className="text-xs font-medium text-text-muted">
                {t(AuditLogsKeys.Browse.FilterSearch)}
              </label>
              <Input
                id="filter-search"
                placeholder={t(AuditLogsKeys.Browse.FilterSearchPlaceholder)}
                value={searchText}
                onChange={(e) => onSearchTextChange(e.target.value)}
              />
            </div>
            <div className="flex items-end gap-2">
              <Button type="submit">{t(AuditLogsKeys.Browse.FilterApply)}</Button>
              {hasActiveFilters && (
                <Button variant="ghost" onClick={onClearFilters}>
                  {t(AuditLogsKeys.Browse.FilterClear)}
                </Button>
              )}
            </div>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}
