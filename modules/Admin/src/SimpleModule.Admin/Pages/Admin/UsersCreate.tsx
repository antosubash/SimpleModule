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
  Container,
  Field,
  FieldGroup,
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
    <Container className="space-y-6">
      <Breadcrumb>
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
      <h1 className="text-2xl font-bold tracking-tight">Create User</h1>

      <Card>
        <CardContent className="p-6">
          <form onSubmit={handleSubmit}>
            <FieldGroup>
              <Field>
                <Label htmlFor="displayName">Display Name</Label>
                <Input id="displayName" name="displayName" required />
              </Field>
              <Field>
                <Label htmlFor="email">Email</Label>
                <Input id="email" name="email" type="email" required />
              </Field>
              <Field>
                <Label htmlFor="password">Password</Label>
                <Input id="password" name="password" type="password" required />
              </Field>
              <Field>
                <Label htmlFor="confirmPassword">Confirm Password</Label>
                <Input id="confirmPassword" name="confirmPassword" type="password" required />
              </Field>
              <Field orientation="horizontal">
                <Checkbox id="emailConfirmed" name="emailConfirmed" value="true" />
                <Label htmlFor="emailConfirmed" className="mb-0">
                  Email confirmed
                </Label>
              </Field>

              {allRoles.length > 0 && (
                <Field>
                  <Label>Roles</Label>
                  <div className="space-y-2">
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
                </Field>
              )}

              <Button type="submit">Create User</Button>
            </FieldGroup>
          </form>
        </CardContent>
      </Card>
    </Container>
  );
}
