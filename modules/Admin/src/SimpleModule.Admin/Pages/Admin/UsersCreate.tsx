import { router } from '@inertiajs/react';
import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Button,
  Card,
  CardContent,
  Checkbox,
  Field,
  FieldGroup,
  Input,
  Label,
  PageShell,
} from '@simplemodule/ui';
import { useState } from 'react';
import { AdminKeys } from '@/Locales/keys';

interface Role {
  id: string;
  name: string;
  description: string | null;
}

interface Props {
  allRoles: Role[];
}

export default function UsersCreate({ allRoles }: Props) {
  const { t } = useTranslation('Admin');
  const [formError, setFormError] = useState<string | null>(null);

  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    if (formData.get('password') !== formData.get('confirmPassword')) {
      setFormError(t(AdminKeys.UsersCreate.ErrorPasswordMismatch));
      return;
    }
    setFormError(null);
    router.post('/admin/users', formData, {
      onError: () => {
        setFormError(t(AdminKeys.UsersCreate.ErrorCreateFailed));
      },
    });
  }

  return (
    <PageShell
      title={t(AdminKeys.UsersCreate.Title)}
      breadcrumbs={[
        { label: t(AdminKeys.UsersCreate.BreadcrumbUsers), href: '/admin/users' },
        { label: t(AdminKeys.UsersCreate.BreadcrumbCreate) },
      ]}
    >
      <Card>
        <CardContent className="p-4 sm:p-6">
          <form onSubmit={handleSubmit}>
            <FieldGroup>
              {formError && (
                <div className="rounded-lg border border-danger/30 bg-danger/10 px-4 py-3 text-sm text-danger">
                  {formError}
                </div>
              )}
              <Field>
                <Label htmlFor="displayName">{t(AdminKeys.UsersCreate.FieldDisplayName)}</Label>
                <Input id="displayName" name="displayName" required />
              </Field>
              <Field>
                <Label htmlFor="email">{t(AdminKeys.UsersCreate.FieldEmail)}</Label>
                <Input id="email" name="email" type="email" required />
              </Field>
              <Field>
                <Label htmlFor="password">{t(AdminKeys.UsersCreate.FieldPassword)}</Label>
                <Input id="password" name="password" type="password" required />
              </Field>
              <Field>
                <Label htmlFor="confirmPassword">
                  {t(AdminKeys.UsersCreate.FieldConfirmPassword)}
                </Label>
                <Input id="confirmPassword" name="confirmPassword" type="password" required />
              </Field>
              <Field orientation="horizontal">
                <Checkbox id="emailConfirmed" name="emailConfirmed" value="true" />
                <Label htmlFor="emailConfirmed" className="mb-0">
                  {t(AdminKeys.UsersCreate.FieldEmailConfirmed)}
                </Label>
              </Field>

              {allRoles.length > 0 && (
                <Field>
                  <Label>{t(AdminKeys.UsersCreate.FieldRoles)}</Label>
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

              <Button type="submit">{t(AdminKeys.UsersCreate.SubmitButton)}</Button>
            </FieldGroup>
          </form>
        </CardContent>
      </Card>
    </PageShell>
  );
}
