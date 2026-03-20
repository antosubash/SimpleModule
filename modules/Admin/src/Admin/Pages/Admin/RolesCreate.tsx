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
  Field,
  FieldGroup,
  Input,
  Label,
} from '@simplemodule/ui';
import { PermissionGroups } from '../components/PermissionGroups';

interface Props {
  permissionsByModule: Record<string, string[]>;
}

export default function RolesCreate({ permissionsByModule }: Props) {
  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    router.post('/admin/roles', new FormData(e.currentTarget));
  }

  return (
    <div className="max-w-xl">
      <Breadcrumb className="mb-4">
        <BreadcrumbList>
          <BreadcrumbItem>
            <BreadcrumbLink href="/admin/roles">Roles</BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbPage>Create Role</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>
      <h1 className="text-2xl font-bold tracking-tight mb-6">Create Role</h1>

      <Card>
        <CardContent className="p-6">
          <form onSubmit={handleSubmit}>
            <FieldGroup className="space-y-6">
              <Field>
                <Label htmlFor="name">Name</Label>
                <Input id="name" name="name" required />
              </Field>
              <Field>
                <Label htmlFor="description">Description</Label>
                <Input id="description" name="description" />
              </Field>
              <Field>
                <Label>Permissions</Label>
                <div className="mt-2">
                  <PermissionGroups
                    permissionsByModule={permissionsByModule}
                    selected={[]}
                    namePrefix="permissions"
                  />
                </div>
              </Field>
              <Button type="submit">Create Role</Button>
            </FieldGroup>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
