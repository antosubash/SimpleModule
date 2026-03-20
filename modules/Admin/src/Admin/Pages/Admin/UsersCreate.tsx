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
  Checkbox,
  Input,
  Label,
} from '@simplemodule/ui';

interface Role {
  id: string;
  name: string;
  description: string | null;
}

interface Props {
  allRoles: Role[];
}

export default function UsersCreate({ allRoles }: Props) {
  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post('/admin/users', formData);
  }

  return (
    <div className="max-w-xl">
      <Breadcrumb className="mb-4">
        <BreadcrumbList>
          <BreadcrumbItem>
            <BreadcrumbLink href="/admin/users">Users</BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbPage>Create User</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>
      <h1 className="text-2xl font-bold tracking-tight mb-6">Create User</h1>

      <Card>
        <CardContent className="p-6">
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <Label htmlFor="displayName">Display Name</Label>
              <Input id="displayName" name="displayName" required />
            </div>
            <div>
              <Label htmlFor="email">Email</Label>
              <Input id="email" name="email" type="email" required />
            </div>
            <div>
              <Label htmlFor="password">Password</Label>
              <Input id="password" name="password" type="password" required />
            </div>
            <div>
              <Label htmlFor="confirmPassword">Confirm Password</Label>
              <Input id="confirmPassword" name="confirmPassword" type="password" required />
            </div>
            <div className="flex items-center gap-2">
              <Checkbox id="emailConfirmed" name="emailConfirmed" value="true" />
              <Label htmlFor="emailConfirmed" className="mb-0">
                Email confirmed
              </Label>
            </div>

            {allRoles.length > 0 && (
              <div>
                <Label>Roles</Label>
                <div className="space-y-2 mt-1">
                  {allRoles.map((role) => (
                    <div key={role.id} className="flex items-center gap-2">
                      <Checkbox id={`role-${role.id}`} name="roles" value={role.name ?? ''} />
                      <Label htmlFor={`role-${role.id}`} className="mb-0">
                        {role.name}
                        {role.description && (
                          <span className="text-text-muted ml-1">&mdash; {role.description}</span>
                        )}
                      </Label>
                    </div>
                  ))}
                </div>
              </div>
            )}

            <Button type="submit">Create User</Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
