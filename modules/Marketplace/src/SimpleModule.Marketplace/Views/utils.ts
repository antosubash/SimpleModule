export function formatDownloads(count: number | undefined | null): string {
  if (count == null) return '0';
  if (count >= 1_000_000) return `${(count / 1_000_000).toFixed(1)}M`;
  if (count >= 1_000) return `${(count / 1_000).toFixed(1)}K`;
  return count.toString();
}

export const categoryNames = [
  'All',
  'Auth',
  'Storage',
  'UI',
  'Analytics',
  'Integration',
  'Communication',
  'Monitoring',
  'Other',
] as const;

export function categoryLabel(value: number | string): string {
  if (typeof value === 'string') return value;
  return categoryNames[value] ?? 'Other';
}
