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
import { TenantsKeys } from '@/Locales/keys';

export default function Create() {
  const { t } = useTranslation('Tenants');

  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post('/api/tenants', formData);
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
            <BreadcrumbPage>{t(TenantsKeys.Create.Breadcrumb)}</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>
      <h1 className="text-2xl font-bold tracking-tight">{t(TenantsKeys.Create.Title)}</h1>

      <Card>
        <CardContent className="p-4 sm:p-6">
          <form onSubmit={handleSubmit}>
            <FieldGroup>
              <Field>
                <Label htmlFor="name">{t(TenantsKeys.Create.NameLabel)}</Label>
                <Input
                  id="name"
                  name="name"
                  required
                  placeholder={t(TenantsKeys.Create.NamePlaceholder)}
                />
              </Field>
              <Field>
                <Label htmlFor="slug">{t(TenantsKeys.Create.SlugLabel)}</Label>
                <Input
                  id="slug"
                  name="slug"
                  required
                  placeholder={t(TenantsKeys.Create.SlugPlaceholder)}
                  pattern="[a-z0-9][a-z0-9-]*[a-z0-9]|[a-z0-9]"
                />
              </Field>
              <Field>
                <Label htmlFor="adminEmail">{t(TenantsKeys.Create.AdminEmailLabel)}</Label>
                <Input
                  id="adminEmail"
                  name="adminEmail"
                  type="email"
                  placeholder={t(TenantsKeys.Create.AdminEmailPlaceholder)}
                />
              </Field>
              <Field>
                <Label htmlFor="editionName">{t(TenantsKeys.Create.EditionLabel)}</Label>
                <Input
                  id="editionName"
                  name="editionName"
                  placeholder={t(TenantsKeys.Create.EditionPlaceholder)}
                />
              </Field>
              <Field>
                <Label htmlFor="validUpTo">{t(TenantsKeys.Create.ValidUntilLabel)}</Label>
                <Input id="validUpTo" name="validUpTo" type="date" />
              </Field>
              <Button type="submit">{t(TenantsKeys.Create.SubmitButton)}</Button>
            </FieldGroup>
          </form>
        </CardContent>
      </Card>
    </Container>
  );
}
