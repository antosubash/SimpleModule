import { Button } from '@simplemodule/ui';

interface ActivityEntry {
  id: number;
  action: string;
  details: string | null;
  performedBy: string;
  timestamp: string;
}

interface ActivityTimelineProps {
  entries: ActivityEntry[];
  total: number;
  onLoadMore?: () => void;
}

const actionLabels: Record<string, string> = {
  UserCreated: 'User created',
  UserUpdated: 'Details updated',
  UserDeactivated: 'Account deactivated',
  UserReactivated: 'Account reactivated',
  RoleAdded: 'Role added',
  RoleRemoved: 'Role removed',
  PermissionGranted: 'Permission granted',
  PermissionRevoked: 'Permission revoked',
  PasswordReset: 'Password reset',
  AccountLocked: 'Account locked',
  AccountUnlocked: 'Account unlocked',
  EmailReverified: 'Email re-verification required',
  TwoFactorDisabled: '2FA disabled',
};

function timeAgo(timestamp: string): string {
  const seconds = Math.floor((Date.now() - new Date(timestamp).getTime()) / 1000);
  if (seconds < 60) return 'just now';
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  if (days < 30) return `${days}d ago`;
  return new Date(timestamp).toLocaleDateString();
}

export function ActivityTimeline({ entries, total, onLoadMore }: ActivityTimelineProps) {
  if (entries.length === 0) {
    return <p className="text-sm text-text-muted">No activity recorded.</p>;
  }

  return (
    <div>
      <div className="space-y-0">
        {entries.map((entry) => (
          <div key={entry.id} className="flex gap-3 py-3 border-b border-border last:border-0">
            <div className="flex-1">
              <p className="text-sm font-medium">
                {actionLabels[entry.action] ?? entry.action}
                {entry.details && (
                  <span className="text-text-muted font-normal"> — {entry.details}</span>
                )}
              </p>
              <p className="text-xs text-text-muted mt-0.5">
                by {entry.performedBy} &middot; {timeAgo(entry.timestamp)}
              </p>
            </div>
          </div>
        ))}
      </div>
      {entries.length < total && onLoadMore && (
        <Button variant="ghost" size="sm" className="mt-4" onClick={onLoadMore}>
          Load more ({total - entries.length} remaining)
        </Button>
      )}
    </div>
  );
}
