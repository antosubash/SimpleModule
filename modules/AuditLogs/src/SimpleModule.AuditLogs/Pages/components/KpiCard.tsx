import { Stat } from '@simplemodule/ui';

/**
 * Thin wrapper around the shared {@link Stat} component, kept for backwards
 * compatibility with existing dashboard call sites. New code should reach for
 * `Stat` directly.
 */
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
    <Stat
      label={title}
      value={accent === 'danger' ? <span className="text-danger">{value}</span> : value}
      change={subtitle}
      interactive={onClick != null}
      onClick={onClick}
    />
  );
}
