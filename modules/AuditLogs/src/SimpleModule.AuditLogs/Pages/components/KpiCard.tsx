import { Card, CardContent } from '@simplemodule/ui';

export function KpiCard({
  title,
  value,
  subtitle,
  accent,
  onClick,
}: {
  title: string;
  value: string;
  subtitle?: string;
  accent?: 'default' | 'danger';
  onClick?: () => void;
}) {
  return (
    <Card className={onClick ? 'cursor-pointer transition-shadow hover:shadow-md' : ''}>
      <CardContent className="p-4 sm:p-5" onClick={onClick}>
        <p className="text-xs font-medium tracking-wide text-text-muted uppercase">{title}</p>
        <p
          className={`mt-1 text-xl sm:text-2xl font-bold tabular-nums ${
            accent === 'danger' ? 'text-danger' : 'text-text'
          }`}
        >
          {value}
        </p>
        {subtitle && <p className="mt-0.5 text-xs text-text-muted">{subtitle}</p>}
      </CardContent>
    </Card>
  );
}
