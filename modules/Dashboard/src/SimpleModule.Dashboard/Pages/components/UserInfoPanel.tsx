import { useTranslation } from '@simplemodule/client/use-translation';
import { Card, CardContent, CardHeader, CardTitle, Spinner } from '@simplemodule/ui';
import React from 'react';
import { DashboardKeys } from '@/Locales/keys';

interface UserInfo {
  displayName?: string;
  name?: string;
  email?: string;
  id?: string | { value: string };
  roles?: string | string[];
}

function InfoRow({
  label,
  value,
  monospace,
}: {
  label: string;
  value: string;
  monospace?: boolean;
}) {
  return (
    <div className="flex flex-col gap-1 py-3 text-sm border-b border-border last:border-b-0 sm:flex-row sm:justify-between sm:items-center sm:gap-0">
      <span className="text-text-muted text-xs uppercase tracking-wide">{label}</span>
      <span
        className={
          monospace ? 'font-mono text-xs text-text-secondary break-all' : 'font-medium text-text'
        }
      >
        {value}
      </span>
    </div>
  );
}

export function UserInfoPanel() {
  const { t } = useTranslation('Dashboard');
  const [userInfo, setUserInfo] = React.useState<UserInfo | null>(null);
  const [error, setError] = React.useState<string | null>(null);
  const [loading, setLoading] = React.useState(true);

  React.useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const res = await fetch('/api/users/me');
        if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);
        const data = await res.json();
        if (!cancelled) {
          setUserInfo(data);
          setLoading(false);
        }
      } catch (e: unknown) {
        if (!cancelled) {
          setError(e instanceof Error ? e.message : String(e));
          setLoading(false);
        }
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t(DashboardKeys.Home.UserInfoTitle)}</CardTitle>
      </CardHeader>
      <CardContent>
        {loading && (
          <div className="text-text-muted text-sm flex items-center gap-2">
            {t(DashboardKeys.Home.UserInfoLoading)}
            <Spinner size="sm" />
          </div>
        )}
        {error && (
          <span className="text-danger text-sm">
            {t(DashboardKeys.Home.UserInfoError, { error })}
          </span>
        )}
        {userInfo && (
          <>
            <InfoRow
              label={t(DashboardKeys.Home.UserInfoLabelName)}
              value={userInfo.displayName || userInfo.name || '-'}
            />
            <InfoRow
              label={t(DashboardKeys.Home.UserInfoLabelEmail)}
              value={userInfo.email || '-'}
            />
            <InfoRow
              label={t(DashboardKeys.Home.UserInfoLabelId)}
              value={
                typeof userInfo.id === 'object' && userInfo.id
                  ? userInfo.id.value
                  : userInfo.id || '-'
              }
              monospace
            />
            {userInfo.roles && (
              <InfoRow
                label={t(DashboardKeys.Home.UserInfoLabelRoles)}
                value={Array.isArray(userInfo.roles) ? userInfo.roles.join(', ') : userInfo.roles}
              />
            )}
          </>
        )}
      </CardContent>
    </Card>
  );
}
