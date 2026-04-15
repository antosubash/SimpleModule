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
import { EmailKeys } from '../../Locales/keys';

type EmailStatus = 'Queued' | 'Sending' | 'Sent' | 'Failed' | 'Retrying';
const STATUS_OPTIONS: EmailStatus[] = ['Queued', 'Sending', 'Sent', 'Failed', 'Retrying'];

interface Props {
  status: string;
  to: string;
  subject: string;
  dateFrom: string;
  dateTo: string;
  hasActiveFilters: boolean;
  onStatusChange: (v: string) => void;
  onToChange: (v: string) => void;
  onSubjectChange: (v: string) => void;
  onDateFromChange: (v: string) => void;
  onDateToChange: (v: string) => void;
  onApplyFilters: (e?: FormEvent) => void;
  onClearFilters: () => void;
}

export function HistoryFilters({
  status,
  to,
  subject,
  dateFrom,
  dateTo,
  hasActiveFilters,
  onStatusChange,
  onToChange,
  onSubjectChange,
  onDateFromChange,
  onDateToChange,
  onApplyFilters,
  onClearFilters,
}: Props) {
  const { t } = useTranslation('Email');
  return (
    <Card>
      <CardContent>
        <form onSubmit={onApplyFilters}>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 sm:gap-4 md:grid-cols-3 lg:grid-cols-4">
            <div className="space-y-1">
              <span className="text-xs font-medium text-text-muted">
                {t(EmailKeys.History.FilterStatus)}
              </span>
              <Select value={status} onValueChange={onStatusChange}>
                <SelectTrigger aria-label={t(EmailKeys.History.FilterStatus)}>
                  <SelectValue placeholder={t(EmailKeys.History.AllStatuses)} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="__all__">{t(EmailKeys.History.AllStatuses)}</SelectItem>
                  {STATUS_OPTIONS.map((s) => (
                    <SelectItem key={s} value={s}>
                      {s}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1">
              <label htmlFor="filter-to" className="text-xs font-medium text-text-muted">
                {t(EmailKeys.History.FilterTo)}
              </label>
              <Input
                id="filter-to"
                placeholder={t(EmailKeys.History.FilterTo)}
                value={to}
                onChange={(e) => onToChange(e.target.value)}
              />
            </div>
            <div className="space-y-1">
              <label htmlFor="filter-subject" className="text-xs font-medium text-text-muted">
                {t(EmailKeys.History.FilterSubject)}
              </label>
              <Input
                id="filter-subject"
                placeholder={t(EmailKeys.History.FilterSubject)}
                value={subject}
                onChange={(e) => onSubjectChange(e.target.value)}
              />
            </div>
            <div className="space-y-1">
              <label htmlFor="filter-date-from" className="text-xs font-medium text-text-muted">
                {t(EmailKeys.History.FilterDateFrom)}
              </label>
              <Input
                id="filter-date-from"
                type="date"
                value={dateFrom}
                onChange={(e) => onDateFromChange(e.target.value)}
              />
            </div>
            <div className="space-y-1">
              <label htmlFor="filter-date-to" className="text-xs font-medium text-text-muted">
                {t(EmailKeys.History.FilterDateTo)}
              </label>
              <Input
                id="filter-date-to"
                type="date"
                value={dateTo}
                onChange={(e) => onDateToChange(e.target.value)}
              />
            </div>
            <div className="flex items-end gap-2">
              <Button type="submit">{t(EmailKeys.History.FilterApply)}</Button>
              {hasActiveFilters && (
                <Button variant="ghost" onClick={onClearFilters}>
                  {t(EmailKeys.History.FilterClear)}
                </Button>
              )}
            </div>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}
