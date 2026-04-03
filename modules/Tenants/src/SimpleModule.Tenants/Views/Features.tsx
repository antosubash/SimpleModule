import { router } from '@inertiajs/react';
import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
  Button,
  Card,
  CardContent,
  Container,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';
import { TenantsKeys } from '../Locales/keys';

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
      `/api/tenants/${tenant.id}/features/${flagName}`,
      { isEnabled: !currentlyEnabled },
      { preserveScroll: true },
    );
  }

  function handleReset(flagName: string) {
    router.delete(`/api/tenants/${tenant.id}/features/${flagName}`, { preserveScroll: true });
  }

  if (flags.length === 0) {
    return (
      <Container className="space-y-4 sm:space-y-6">
        <h1 className="text-2xl font-bold tracking-tight">{t(TenantsKeys.Features.EmptyTitle)}</h1>
        <p className="text-text-muted">{t(TenantsKeys.Features.EmptyDescription)}</p>
      </Container>
    );
  }

  return (
    <Container className="space-y-4 sm:space-y-6">
      <Breadcrumb>
        <BreadcrumbList>
          <BreadcrumbItem>
            <BreadcrumbLink href="/tenants/manage">{t(TenantsKeys.Manage.Title)}</BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbLink href={`/tenants/${tenant.id}/edit`}>{tenant.name}</BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbPage>{t(TenantsKeys.Features.Breadcrumb)}</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>
      <h1 className="text-2xl font-bold tracking-tight">
        {t(TenantsKeys.Features.Title, { name: tenant.name })}
      </h1>

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
                          <span className={flag.isEnabled ? 'text-green-600' : 'text-red-600'}>
                            {flag.isEnabled
                              ? t(TenantsKeys.Features.On)
                              : t(TenantsKeys.Features.Off)}
                          </span>
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
    </Container>
  );
}
