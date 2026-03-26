import type * as React from 'react';
import { DayPicker } from 'react-day-picker';
import { cn } from '../lib/utils';

export type CalendarProps = React.ComponentProps<typeof DayPicker>;

function Calendar({ className, classNames, showOutsideDays = true, ...props }: CalendarProps) {
  return (
    <DayPicker
      showOutsideDays={showOutsideDays}
      className={cn('p-3', className)}
      classNames={{
        months: 'flex flex-col sm:flex-row space-y-4 sm:space-x-4 sm:space-y-0',
        month: 'space-y-4',
        month_caption: 'flex justify-center pt-1 relative items-center',
        caption_label: 'text-sm font-medium',
        nav: 'space-x-1 flex items-center',
        button_previous:
          'absolute left-1 inline-flex items-center justify-center rounded-md text-sm font-medium ring-offset-surface transition-colors hover:bg-surface-sunken hover:text-text focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 h-7 w-7 bg-transparent p-0 opacity-50 hover:opacity-100',
        button_next:
          'absolute right-1 inline-flex items-center justify-center rounded-md text-sm font-medium ring-offset-surface transition-colors hover:bg-surface-sunken hover:text-text focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 h-7 w-7 bg-transparent p-0 opacity-50 hover:opacity-100',
        month_grid: 'w-full border-collapse space-y-1',
        weekdays: 'flex',
        weekday: 'text-text-muted rounded-md w-9 font-normal text-[0.8rem]',
        week: 'flex w-full mt-2',
        day: 'h-9 w-9 text-center text-sm p-0 relative [&:has([aria-selected].day-range-end)]:rounded-r-md [&:has([aria-selected].day-outside)]:bg-primary-subtle/50 [&:has([aria-selected])]:bg-primary-subtle first:[&:has([aria-selected])]:rounded-l-md last:[&:has([aria-selected])]:rounded-r-md focus-within:relative focus-within:z-20',
        day_button:
          'inline-flex items-center justify-center rounded-md text-sm ring-offset-surface transition-colors hover:bg-surface-sunken hover:text-text focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 h-9 w-9 p-0 font-normal aria-selected:opacity-100',
        range_end: 'day-range-end',
        selected:
          'bg-primary text-text-inverse hover:bg-primary hover:text-text-inverse focus:bg-primary focus:text-text-inverse',
        today: 'bg-surface-sunken text-text',
        outside:
          'day-outside text-text-muted aria-selected:bg-primary-subtle/50 aria-selected:text-text-muted',
        disabled: 'text-text-muted opacity-50',
        range_middle: 'aria-selected:bg-primary-subtle aria-selected:text-text',
        hidden: 'invisible',
        ...classNames,
      }}
      {...props}
    />
  );
}
Calendar.displayName = 'Calendar';

export { Calendar };
