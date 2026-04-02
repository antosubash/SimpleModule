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
  Field,
  FieldGroup,
  Input,
  Label,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';
import { useState } from 'react';
import type { Tenant } from '../types';
import { statusLabels } from './tenantStatus';

export default function Edit({ tenant }: { tenant: Tenant }) {
  const [newHost, setNewHost] = useState('');

  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.put(`/api/tenants/${tenant.id}`, Object.fromEntries(formData));
  }

  function handleAddHost() {
    if (!newHost.trim()) return;
    router.post(
      `/api/tenants/${tenant.id}/hosts`,
      { hostName: newHost.trim() },
      { preserveScroll: true },
    );
    setNewHost('');
  }

  function handleRemoveHost(hostId: number) {
    router.delete(`/api/tenants/${tenant.id}/hosts/${hostId}`, { preserveScroll: true });
  }

  function handleStatusChange(status: number) {
    router.put(`/api/tenants/${tenant.id}/status`, { status }, { preserveScroll: true });
  }

  return (
    <Container className="space-y-4 sm:space-y-6">
      <Breadcrumb>
        <BreadcrumbList>
          <BreadcrumbItem>
            <BreadcrumbLink href="/tenants/manage">Tenants</BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbPage>Edit {tenant.name}</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
        <h1 className="text-2xl font-bold tracking-tight">Edit Tenant</h1>
        <Button variant="ghost" onClick={() => router.get(`/tenants/${tenant.id}/features`)}>
          Manage Features
        </Button>
      </div>

      <Card>
        <CardContent className="p-4 sm:p-6">
          <form onSubmit={handleSubmit}>
            <FieldGroup>
              <Field>
                <Label htmlFor="name">Name</Label>
                <Input id="name" name="name" required defaultValue={tenant.name} />
              </Field>
              <Field>
                <Label htmlFor="adminEmail">Admin Email</Label>
                <Input
                  id="adminEmail"
                  name="adminEmail"
                  type="email"
                  defaultValue={tenant.adminEmail ?? ''}
                />
              </Field>
              <Field>
                <Label htmlFor="editionName">Edition</Label>
                <Input
                  id="editionName"
                  name="editionName"
                  defaultValue={tenant.editionName ?? ''}
                />
              </Field>
              <Field>
                <Label htmlFor="validUpTo">Valid Until</Label>
                <Input
                  id="validUpTo"
                  name="validUpTo"
                  type="date"
                  defaultValue={tenant.validUpTo?.split('T')[0] ?? ''}
                />
              </Field>
              <Button type="submit">Save Changes</Button>
            </FieldGroup>
          </form>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="p-4 sm:p-6">
          <h2 className="text-lg font-semibold mb-4">Status</h2>
          <div className="flex flex-wrap gap-2">
            {[0, 1, 2].map((s) => (
              <Button
                key={s}
                variant={tenant.status === s ? 'primary' : 'secondary'}
                size="sm"
                onClick={() => handleStatusChange(s)}
              >
                {statusLabels[s]}
              </Button>
            ))}
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="p-4 sm:p-6">
          <h2 className="text-lg font-semibold mb-4">Hosts</h2>
          {tenant.hosts.length > 0 && (
            <div className="overflow-x-auto -mx-4 px-4 sm:mx-0 sm:px-0">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Host Name</TableHead>
                    <TableHead>Active</TableHead>
                    <TableHead />
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {tenant.hosts.map((host) => (
                    <TableRow key={host.id}>
                      <TableCell className="font-mono text-sm">{host.hostName}</TableCell>
                      <TableCell>{host.isActive ? 'Yes' : 'No'}</TableCell>
                      <TableCell>
                        <Button
                          variant="danger"
                          size="sm"
                          onClick={() => handleRemoveHost(host.id)}
                        >
                          Remove
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
          <div className="flex flex-col gap-2 sm:flex-row mt-4">
            <Input
              placeholder="new-host.example.com"
              value={newHost}
              onChange={(e) => setNewHost(e.target.value)}
            />
            <Button onClick={handleAddHost}>Add Host</Button>
          </div>
        </CardContent>
      </Card>
    </Container>
  );
}
