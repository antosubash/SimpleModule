export const PALETTE = [
  'var(--color-primary)',
  'var(--color-info)',
  'var(--color-warning)',
  'var(--color-danger)',
  'var(--color-success-light)',
  'var(--color-accent)',
  'var(--color-muted)',
  'var(--color-primary-light)',
];

export const SOURCE_COLORS: Record<string, string> = {
  Http: 'var(--color-info)',
  Domain: 'var(--color-primary)',
  ChangeTracker: 'var(--color-warning)',
};

export const STATUS_COLORS: Record<string, string> = {
  '2xx': 'var(--color-success)',
  '3xx': 'var(--color-info)',
  '4xx': 'var(--color-warning)',
  '5xx': 'var(--color-danger)',
  Other: 'var(--color-muted)',
};

export const DATE_PRESETS = [
  { label: 'Last 24h', hours: 24 },
  { label: 'Last 7 days', hours: 168 },
  { label: 'Last 30 days', hours: 720 },
  { label: 'Last 90 days', hours: 2160 },
];

export function dictToChartData(dict: Record<string, number>): { name: string; value: number }[] {
  return Object.entries(dict)
    .map(([name, value]) => ({ name, value }))
    .sort((a, b) => b.value - a.value);
}

export function formatDate(iso: string): string {
  return iso.slice(0, 10);
}
