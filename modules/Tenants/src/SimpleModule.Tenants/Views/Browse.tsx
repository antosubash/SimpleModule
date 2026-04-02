import { Card, CardContent, PageShell } from '@simplemodule/ui';
import { statusColors, statusLabels } from './tenantStatus';

interface BrowseTenant {
  id: number;
  name: string;
  slug: string;
  status: number;
  hostCount: number;
}

export default function Browse({ tenants }: { tenants: BrowseTenant[] }) {
  return (
    <PageShell title="Tenants" description="Browse all tenants.">
      <div className="space-y-3">
        {tenants.map((t) => (
          <Card key={t.id} data-testid="tenant-card">
            <CardContent className="flex flex-col gap-2 sm:flex-row sm:justify-between sm:items-center">
              <div>
                <span className="font-medium">{t.name}</span>
                <span className="text-text-muted ml-2">({t.slug})</span>
              </div>
              <div className="flex items-center gap-3 sm:gap-4">
                <span className="text-text-muted text-sm">
                  {t.hostCount} host{t.hostCount !== 1 ? 's' : ''}
                </span>
                <span className={`text-sm font-medium ${statusColors[t.status]}`}>
                  {statusLabels[t.status]}
                </span>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
    </PageShell>
  );
}
