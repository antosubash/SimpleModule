import { router } from '@inertiajs/react';
import { routes } from '@simplemodule/client/routes';
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
import { TenantsKeys } from '@/Locales/keys';

export default function Create() {
  const { t } = useTranslation('Tenants');

  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post(routes.tenants.api.create(), formData);
  }

  return (
    <PageShell
      title={t(TenantsKeys.Create.Title)}
      breadcrumbs={[
        { label: t(TenantsKeys.Manage.Title), href: routes.tenants.views.manage() },
        { label: t(TenantsKeys.Create.Breadcrumb) },
      ]}
    >
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
    </PageShell>
  );
}
