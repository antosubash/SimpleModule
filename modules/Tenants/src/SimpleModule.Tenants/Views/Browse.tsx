import { Card, CardContent, PageShell } from '@simplemodule/ui';

interface BrowseTenant {
  id: number;
  name: string;
  slug: string;
  status: number;
  hostCount: number;
}

const statusLabels: Record<number, string> = { 0: 'Active', 1: 'Suspended', 2: 'Inactive' };
const statusColors: Record<number, string> = {
  0: 'text-green-600',
  1: 'text-yellow-600',
  2: 'text-red-600',
};

export default function Browse({ tenants }: { tenants: BrowseTenant[] }) {
  return (
    <PageShell title="Tenants" description="Browse all tenants.">
      <div className="space-y-3">
        {tenants.map((t) => (
          <Card key={t.id} data-testid="tenant-card">
            <CardContent className="flex justify-between items-center">
              <div>
                <span className="font-medium">{t.name}</span>
                <span className="text-text-muted ml-2">({t.slug})</span>
              </div>
              <div className="flex items-center gap-4">
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
