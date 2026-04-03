import { useTranslation } from '@simplemodule/client/use-translation';
import { Card, CardContent, PageShell } from '@simplemodule/ui';
import { TenantsKeys } from '@/Locales/keys';
import { statusColors, statusLabels } from './tenantStatus';

interface BrowseTenant {
  id: number;
  name: string;
  slug: string;
  status: number;
  hostCount: number;
}

export default function Browse({ tenants }: { tenants: BrowseTenant[] }) {
  const { t } = useTranslation('Tenants');

  return (
    <PageShell title={t(TenantsKeys.Browse.Title)} description={t(TenantsKeys.Browse.Description)}>
      <div className="space-y-3">
        {tenants.map((tenant) => (
          <Card key={tenant.id} data-testid="tenant-card">
            <CardContent className="flex flex-col gap-2 sm:flex-row sm:justify-between sm:items-center">
              <div>
                <span className="font-medium">{tenant.name}</span>
                <span className="text-text-muted ml-2">({tenant.slug})</span>
              </div>
              <div className="flex items-center gap-3 sm:gap-4">
                <span className="text-text-muted text-sm">
                  {tenant.hostCount}{' '}
                  {t(
                    tenant.hostCount !== 1
                      ? TenantsKeys.Browse.HostCount_other
                      : TenantsKeys.Browse.HostCount_one,
                    { count: tenant.hostCount },
                  )}
                </span>
                <span className={`text-sm font-medium ${statusColors[tenant.status]}`}>
                  {statusLabels[tenant.status]}
                </span>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
    </PageShell>
  );
}
