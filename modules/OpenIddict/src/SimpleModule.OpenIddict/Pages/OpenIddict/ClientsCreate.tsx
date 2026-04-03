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
  FieldDescription,
  FieldGroup,
  Input,
  Label,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@simplemodule/ui';
import { useState } from 'react';
import { OpenIddictKeys } from '../../Locales/keys';

export default function ClientsCreate() {
  const { t } = useTranslation('OpenIddict');
  const [clientType, setClientType] = useState('public');

  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    router.post('/openiddict/clients', new FormData(e.currentTarget));
  }

  return (
    <Container className="space-y-4 sm:space-y-6">
      <Breadcrumb>
        <BreadcrumbList>
          <BreadcrumbItem>
            <BreadcrumbLink href="/openiddict/clients">
              {t(OpenIddictKeys.ClientsCreate.Breadcrumb)}
            </BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbPage>{t(OpenIddictKeys.ClientsCreate.BreadcrumbPage)}</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>
      <h1 className="text-2xl font-bold tracking-tight">{t(OpenIddictKeys.ClientsCreate.Title)}</h1>

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
    </Container>
  );
}
