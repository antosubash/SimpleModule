import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Button,
  DatePicker,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@simplemodule/ui';
import { AuditLogsKeys } from '@/Locales/keys';
import type { NamedCount } from '@/types';
import { DATE_PRESETS } from './dashboard-constants';

interface Props {
  dateFrom: Date | undefined;
  dateTo: Date | undefined;
  onDateFromChange: (date: Date | undefined) => void;
  onDateToChange: (date: Date | undefined) => void;
  selectedUser: string;
  onSelectedUserChange: (value: string) => void;
  users: NamedCount[];
  onApplyFilters: () => void;
  onApplyDatePreset: (hours: number) => void;
}

export function DashboardFilters({
  dateFrom,
  dateTo,
  onDateFromChange,
  onDateToChange,
  selectedUser,
  onSelectedUserChange,
  users,
  onApplyFilters,
  onApplyDatePreset,
}: Props) {
  const { t } = useTranslation('AuditLogs');

  return (
    <div className="flex flex-col gap-2 sm:flex-row sm:flex-wrap sm:items-end sm:gap-2">
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
      <div className="space-y-1">
        <span className="text-xs font-medium text-text-muted">
          {t(AuditLogsKeys.Dashboard.FilterFrom)}
        </span>
        <DatePicker
          value={dateFrom}
          onChange={onDateFromChange}
          placeholder={t(AuditLogsKeys.Dashboard.FilterFromPlaceholder)}
        />
      </div>
      <div className="space-y-1">
        <span className="text-xs font-medium text-text-muted">
          {t(AuditLogsKeys.Dashboard.FilterTo)}
        </span>
        <DatePicker
          value={dateTo}
          onChange={onDateToChange}
          placeholder={t(AuditLogsKeys.Dashboard.FilterToPlaceholder)}
        />
      </div>
      <div className="space-y-1">
        <span className="text-xs font-medium text-text-muted">
          {t(AuditLogsKeys.Dashboard.FilterUser)}
        </span>
        <Select value={selectedUser} onValueChange={onSelectedUserChange}>
          <SelectTrigger
            className="w-full sm:w-[180px]"
            aria-label={t(AuditLogsKeys.Dashboard.FilterUser)}
          >
            <SelectValue placeholder={t(AuditLogsKeys.Dashboard.FilterUserAll)} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="__all__">{t(AuditLogsKeys.Dashboard.FilterUserAll)}</SelectItem>
            {users.map((u) => (
              <SelectItem key={u.name} value={u.name}>
                {u.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>
      <Button onClick={onApplyFilters}>{t(AuditLogsKeys.Dashboard.FilterApply)}</Button>
    </div>
  );
}
