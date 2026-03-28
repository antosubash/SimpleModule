export const SOURCE_LABELS: Record<number, string> = {
  0: 'HTTP',
  1: 'Domain',
  2: 'Changes',
};

export const ACTION_LABELS: Record<number, string> = {
  0: 'Created',
  1: 'Updated',
  2: 'Deleted',
  3: 'Viewed',
  4: 'Login OK',
  5: 'Login Fail',
  6: 'Perm Granted',
  7: 'Perm Revoked',
  8: 'Setting Changed',
  9: 'Exported',
  10: 'Other',
};

export function sourceBadgeVariant(source: number) {
  if (source === 1) return 'success' as const;
  if (source === 2) return 'warning' as const;
  return 'default' as const;
}

export function actionBadgeVariant(action: number | null | undefined) {
  if (action == null) return 'default' as const;
  if (action === 0) return 'success' as const;
  if (action === 2) return 'danger' as const;
  if (action === 5 || action === 7) return 'warning' as const;
  return 'info' as const;
}

export function statusBadgeVariant(statusCode: number | null | undefined) {
  if (statusCode == null) return 'default' as const;
  if (statusCode >= 200 && statusCode < 300) return 'success' as const;
  if (statusCode >= 400 && statusCode < 500) return 'warning' as const;
  if (statusCode >= 500) return 'danger' as const;
  return 'default' as const;
}

export function formatTimestamp(iso: string): string {
  const d = new Date(iso);
  return d.toLocaleString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  });
}

export function relativeTime(iso: string): string {
  const now = Date.now();
  const then = new Date(iso).getTime();
  const diffSec = Math.floor((now - then) / 1000);
  if (diffSec < 60) return 'just now';
  const diffMin = Math.floor(diffSec / 60);
  if (diffMin < 60) return `${diffMin}m ago`;
  const diffHr = Math.floor(diffMin / 60);
  if (diffHr < 24) return `${diffHr}h ago`;
  const diffDay = Math.floor(diffHr / 24);
  if (diffDay < 30) return `${diffDay}d ago`;
  return formatTimestamp(iso);
}
