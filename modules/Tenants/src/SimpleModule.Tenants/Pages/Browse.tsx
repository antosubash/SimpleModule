import { useTranslation } from '@simplemodule/client/use-translation';
import { Badge, Card, CardContent, EmptyState, PageShell } from '@simplemodule/ui';
import { TenantsKeys } from '@/Locales/keys';
import { statusLabels, statusVariant } from './tenantStatus';

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
      {tenants.length === 0 ? (
        <EmptyState
          title={t(TenantsKeys.Manage.EmptyTitle)}
          description={t(TenantsKeys.Manage.EmptyDescription)}
        />
      ) : (
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
                      { count: String(tenant.hostCount) },
                    )}
                  </span>
                  <Badge variant={statusVariant[tenant.status]}>
                    {statusLabels[tenant.status]}
                  </Badge>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </PageShell>
  );
}
