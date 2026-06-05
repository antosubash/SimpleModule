import { router } from '@inertiajs/react';
import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Button,
  Card,
  CardContent,
  Field,
  FieldGroup,
  Input,
  Label,
  PageShell,
} from '@simplemodule/ui';
import { PermissionGroups } from '@/components/PermissionGroups';
import { AdminKeys } from '@/Locales/keys';

interface Props {
  permissionsByModule: Record<string, string[]>;
}

export default function RolesCreate({ permissionsByModule }: Props) {
  const { t } = useTranslation('Admin');

  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    router.post('/admin/roles', new FormData(e.currentTarget));
  }

  return (
    <PageShell
      title={t(AdminKeys.RolesCreate.Title)}
      breadcrumbs={[
        { label: t(AdminKeys.RolesCreate.BreadcrumbRoles), href: '/admin/roles' },
        { label: t(AdminKeys.RolesCreate.BreadcrumbCreate) },
      ]}
    >
      <Card>
        <CardContent className="p-4 sm:p-6">
          <form onSubmit={handleSubmit}>
            <FieldGroup className="space-y-4 sm:space-y-6">
              <Field>
                <Label htmlFor="name">{t(AdminKeys.RolesCreate.FieldName)}</Label>
                <Input id="name" name="name" required />
              </Field>
              <Field>
                <Label htmlFor="description">{t(AdminKeys.RolesCreate.FieldDescription)}</Label>
                <Input id="description" name="description" />
              </Field>
              <Field>
                <Label>{t(AdminKeys.RolesCreate.FieldPermissions)}</Label>
                <div className="mt-2">
                  <PermissionGroups
                    permissionsByModule={permissionsByModule}
                    selected={[]}
                    namePrefix="permissions"
                  />
                </div>
              </Field>
              <Button type="submit">{t(AdminKeys.RolesCreate.SubmitButton)}</Button>
            </FieldGroup>
          </form>
        </CardContent>
      </Card>
    </PageShell>
  );
}
