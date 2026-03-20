import { router } from '@inertiajs/react';
import { Button, Card, CardContent, Field, FieldGroup, Input, Label } from '@simplemodule/ui';

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
      <div className="flex items-center gap-3 mb-1">
        <a
          href="/admin/users"
          className="text-text-muted hover:text-text transition-colors no-underline"
        >
          <svg
            className="w-4 h-4"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            viewBox="0 0 24 24"
          >
            <path d="M15 19l-7-7 7-7" />
          </svg>
        </a>
        <h1 className="text-2xl font-extrabold tracking-tight">Create User</h1>
      </div>
      <p className="text-text-muted text-sm ml-7 mb-6">Add a new user account</p>

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
                <Label htmlFor="emailConfirmed" className="mb-0">
                  Email confirmed
                </Label>
                <input
                  type="checkbox"
                  name="emailConfirmed"
                  id="emailConfirmed"
                  className="h-5 w-5 rounded-md border border-border bg-surface accent-primary"
                />
              </Field>

              {allRoles.length > 0 && (
                <Field>
                  <Label>Roles</Label>
                  <div className="space-y-2">
                    {allRoles.map((role) => (
                      <div key={role.id} className="flex items-center gap-2">
                        <input
                          type="checkbox"
                          name="roles"
                          value={role.name ?? ''}
                          id={`role-${role.id}`}
                          className="h-5 w-5 rounded-md border border-border bg-surface accent-primary"
                        />
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
    </div>
  );
}
