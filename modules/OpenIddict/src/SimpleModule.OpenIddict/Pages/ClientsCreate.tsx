import { router } from '@inertiajs/react';
import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Button,
  Card,
  CardContent,
  Field,
  FieldDescription,
  FieldGroup,
  Input,
  Label,
  PageShell,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@simplemodule/ui';
import { useState } from 'react';
import { OpenIddictKeys } from '@/Locales/keys';

export default function ClientsCreate() {
  const { t } = useTranslation('OpenIddict');
  const [clientType, setClientType] = useState('public');

  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    router.post('/openiddict/clients', new FormData(e.currentTarget));
  }

  return (
    <PageShell
      title={t(OpenIddictKeys.ClientsCreate.Title)}
      breadcrumbs={[
        { label: t(OpenIddictKeys.ClientsCreate.Breadcrumb), href: '/openiddict/clients' },
        { label: t(OpenIddictKeys.ClientsCreate.BreadcrumbPage) },
      ]}
    >
      <Card>
        <CardContent className="p-4 sm:p-6">
          <form onSubmit={handleSubmit}>
            <FieldGroup className="space-y-4 sm:space-y-6">
              <Field>
                <Label htmlFor="clientId">{t(OpenIddictKeys.ClientsCreate.ClientIdLabel)}</Label>
                <Input
                  id="clientId"
                  name="clientId"
                  required
                  placeholder={t(OpenIddictKeys.ClientsCreate.ClientIdPlaceholder)}
                />
              </Field>
              <Field>
                <Label htmlFor="displayName">
                  {t(OpenIddictKeys.ClientsCreate.DisplayNameLabel)}
                </Label>
                <Input
                  id="displayName"
                  name="displayName"
                  placeholder={t(OpenIddictKeys.ClientsCreate.DisplayNamePlaceholder)}
                />
              </Field>
              <Field>
                <Label htmlFor="clientType">
                  {t(OpenIddictKeys.ClientsCreate.ClientTypeLabel)}
                </Label>
                <Select value={clientType} onValueChange={setClientType} name="clientType">
                  <SelectTrigger id="clientType">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="public">
                      {t(OpenIddictKeys.ClientsCreate.ClientTypePublic)}
                    </SelectItem>
                    <SelectItem value="confidential">
                      {t(OpenIddictKeys.ClientsCreate.ClientTypeConfidential)}
                    </SelectItem>
                  </SelectContent>
                </Select>
              </Field>
              {clientType === 'confidential' && (
                <Field>
                  <Label htmlFor="clientSecret">
                    {t(OpenIddictKeys.ClientsCreate.ClientSecretLabel)}
                  </Label>
                  <Input
                    id="clientSecret"
                    name="clientSecret"
                    type="password"
                    placeholder={t(OpenIddictKeys.ClientsCreate.ClientSecretPlaceholder)}
                  />
                  <FieldDescription>
                    {t(OpenIddictKeys.ClientsCreate.ClientSecretDescription)}
                  </FieldDescription>
                </Field>
              )}
              <Button type="submit">{t(OpenIddictKeys.ClientsCreate.SubmitButton)}</Button>
            </FieldGroup>
          </form>
        </CardContent>
      </Card>
    </PageShell>
  );
}
