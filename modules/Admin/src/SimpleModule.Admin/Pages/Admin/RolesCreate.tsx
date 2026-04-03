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
  Field,
  FieldGroup,
  Input,
  Label,
} from '@simplemodule/ui';
import { AdminKeys } from '../../Locales/keys';
import { PermissionGroups } from '../components/PermissionGroups';

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
    <Container className="space-y-4 sm:space-y-6">
      <Breadcrumb>
        <BreadcrumbList>
          <BreadcrumbItem>
            <BreadcrumbLink href="/admin/roles">
              {t(AdminKeys.RolesCreate.BreadcrumbRoles)}
            </BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbPage>{t(AdminKeys.RolesCreate.BreadcrumbCreate)}</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>
      <h1 className="text-2xl font-bold tracking-tight">{t(AdminKeys.RolesCreate.Title)}</h1>

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
    </Container>
  );
}
