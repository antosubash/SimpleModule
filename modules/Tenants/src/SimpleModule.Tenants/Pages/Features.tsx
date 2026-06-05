import { router } from '@inertiajs/react';
import { routes } from '@simplemodule/client/routes';
import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Badge,
  Button,
  Card,
  CardContent,
  EmptyState,
  PageShell,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';
import { TenantsKeys } from '@/Locales/keys';

interface FeatureFlag {
  name: string;
  description: string;
  isEnabled: boolean;
  defaultEnabled: boolean;
  isDeprecated: boolean;
}

interface FeatureFlagOverride {
  id: number;
  flagName: string;
  overrideType: number;
  overrideValue: string;
  isEnabled: boolean;
}

interface Tenant {
  id: number;
  name: string;
  slug: string;
}

interface Props {
  tenant: Tenant;
  flags: FeatureFlag[];
  tenantOverrides: FeatureFlagOverride[];
}

export default function Features({ tenant, flags, tenantOverrides }: Props) {
  const { t } = useTranslation('Tenants');
  const overrideMap = new Map(tenantOverrides.map((o) => [o.flagName, o]));

  function handleToggle(flagName: string, currentlyEnabled: boolean) {
    router.put(
      routes.tenants.api.setTenantFeature(tenant.id, flagName),
      { isEnabled: !currentlyEnabled },
      { preserveScroll: true },
    );
  }

  function handleReset(flagName: string) {
    router.delete(routes.tenants.api.deleteTenantFeature(tenant.id, flagName), {
      preserveScroll: true,
    });
  }

  const breadcrumbs = [
    { label: t(TenantsKeys.Manage.Title), href: routes.tenants.views.manage() },
    { label: tenant.name, href: routes.tenants.views.edit(tenant.id) },
    { label: t(TenantsKeys.Features.Breadcrumb) },
  ];

  if (flags.length === 0) {
    return (
      <PageShell
        title={t(TenantsKeys.Features.Title, { name: tenant.name })}
        breadcrumbs={breadcrumbs}
      >
        <EmptyState
          title={t(TenantsKeys.Features.EmptyTitle)}
          description={t(TenantsKeys.Features.EmptyDescription)}
        />
      </PageShell>
    );
  }

  return (
    <PageShell
      title={t(TenantsKeys.Features.Title, { name: tenant.name })}
      breadcrumbs={breadcrumbs}
    >
      <Card>
        <CardContent className="p-4 sm:p-6">
          <div className="overflow-x-auto -mx-4 px-4 sm:mx-0 sm:px-0">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t(TenantsKeys.Features.ColFlag)}</TableHead>
                  <TableHead>{t(TenantsKeys.Features.ColDescription)}</TableHead>
                  <TableHead>{t(TenantsKeys.Features.ColGlobal)}</TableHead>
                  <TableHead>{t(TenantsKeys.Features.ColTenantOverride)}</TableHead>
                  <TableHead />
                </TableRow>
              </TableHeader>
              <TableBody>
                {flags
                  .filter((f) => !f.isDeprecated)
                  .map((flag) => {
                    const override = overrideMap.get(flag.name);
                    const effectiveState = override ? override.isEnabled : flag.isEnabled;

                    return (
                      <TableRow key={flag.name}>
                        <TableCell className="font-mono text-sm">{flag.name}</TableCell>
                        <TableCell className="text-text-muted">{flag.description || '-'}</TableCell>
                        <TableCell>
                          <Badge variant={flag.isEnabled ? 'success' : 'default'}>
                            {flag.isEnabled
                              ? t(TenantsKeys.Features.On)
                              : t(TenantsKeys.Features.Off)}
                          </Badge>
                        </TableCell>
                        <TableCell>
                          <Button
                            variant={effectiveState ? 'primary' : 'secondary'}
                            size="sm"
                            onClick={() => handleToggle(flag.name, effectiveState)}
                          >
                            {effectiveState
                              ? t(TenantsKeys.Features.Enabled)
                              : t(TenantsKeys.Features.Disabled)}
                          </Button>
                        </TableCell>
                        <TableCell>
                          {override && (
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => handleReset(flag.name)}
                            >
                              {t(TenantsKeys.Features.ResetButton)}
                            </Button>
                          )}
                        </TableCell>
                      </TableRow>
                    );
                  })}
              </TableBody>
            </Table>
          </div>
        </CardContent>
      </Card>
    </PageShell>
  );
}
