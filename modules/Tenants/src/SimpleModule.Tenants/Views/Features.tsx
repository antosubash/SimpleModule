import { router } from '@inertiajs/react';
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
      <Container className="space-y-6">
        <h1 className="text-2xl font-bold tracking-tight">No Feature Flags Available</h1>
        <p className="text-text-muted">
          The Feature Flags module is not installed or has no flags.
        </p>
      </Container>
    );
  }

  return (
    <Container className="space-y-6">
      <Breadcrumb>
        <BreadcrumbList>
          <BreadcrumbItem>
            <BreadcrumbLink href="/tenants/manage">Tenants</BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbLink href={`/tenants/${tenant.id}/edit`}>{tenant.name}</BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbPage>Feature Flags</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>
      <h1 className="text-2xl font-bold tracking-tight">Feature Flags for {tenant.name}</h1>

      <Card>
        <CardContent className="p-6">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Flag</TableHead>
                <TableHead>Description</TableHead>
                <TableHead>Global</TableHead>
                <TableHead>Tenant Override</TableHead>
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
                          {flag.isEnabled ? 'On' : 'Off'}
                        </span>
                      </TableCell>
                      <TableCell>
                        <Button
                          variant={effectiveState ? 'primary' : 'secondary'}
                          size="sm"
                          onClick={() => handleToggle(flag.name, effectiveState)}
                        >
                          {effectiveState ? 'Enabled' : 'Disabled'}
                        </Button>
                      </TableCell>
                      <TableCell>
                        {override && (
                          <Button variant="ghost" size="sm" onClick={() => handleReset(flag.name)}>
                            Reset
                          </Button>
                        )}
                      </TableCell>
                    </TableRow>
                  );
                })}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </Container>
  );
}
