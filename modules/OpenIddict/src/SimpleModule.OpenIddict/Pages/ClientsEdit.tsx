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
  CardHeader,
  CardTitle,
  Checkbox,
  Container,
  Field,
  FieldGroup,
  Input,
  Label,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Tabs,
  TabsList,
  TabsTrigger,
} from '@simplemodule/ui';
import { useState } from 'react';
import { OpenIddictKeys } from '@/Locales/keys';

interface ClientDetail {
  id: string;
  clientId: string;
  displayName: string | null;
  clientType: string | null;
}

interface Props {
  client: ClientDetail;
  redirectUris: string[];
  postLogoutUris: string[];
  permissions: string[];
  tab: string;
}

const tabIds = [
  { id: 'details', key: 'TabDetails' as const },
  { id: 'uris', key: 'TabUris' as const },
  { id: 'permissions', key: 'TabPermissions' as const },
];

const permissionGroupDefs = [
  {
    key: 'Endpoints' as const,
    permissions: [
      { value: 'ept:authorization', key: 'Authorization' as const },
      { value: 'ept:token', key: 'Token' as const },
      { value: 'ept:end_session', key: 'EndSession' as const },
      { value: 'ept:revocation', key: 'Revocation' as const },
      { value: 'ept:introspection', key: 'Introspection' as const },
    ],
  },
  {
    key: 'GrantTypes' as const,
    permissions: [
      { value: 'gt:authorization_code', key: 'AuthorizationCode' as const },
      { value: 'gt:refresh_token', key: 'RefreshToken' as const },
      { value: 'gt:client_credentials', key: 'ClientCredentials' as const },
      { value: 'gt:implicit', key: 'Implicit' as const },
    ],
  },
  {
    key: 'ResponseTypes' as const,
    permissions: [
      { value: 'rst:code', key: 'Code' as const },
      { value: 'rst:token', key: 'TokenResponse' as const },
    ],
  },
  {
    key: 'Scopes' as const,
    permissions: [
      { value: 'scp:openid', key: 'OpenID' as const },
      { value: 'scp:profile', key: 'Profile' as const },
      { value: 'scp:email', key: 'Email' as const },
      { value: 'scp:roles', key: 'Roles' as const },
    ],
  },
];

function UriList({ label, name, values }: { label: string; name: string; values: string[] }) {
  const { t } = useTranslation('OpenIddict');
  const [uris, setUris] = useState(values.length > 0 ? values : ['']);

  function addUri() {
    setUris([...uris, '']);
  }

  function removeUri(index: number) {
    setUris(uris.filter((_, i) => i !== index));
  }

  function updateUri(index: number, value: string) {
    const updated = [...uris];
    updated[index] = value;
    setUris(updated);
  }

  return (
    <Field>
      <Label>{label}</Label>
      <div className="space-y-2">
        {uris.map((uri, index) => (
          // biome-ignore lint/suspicious/noArrayIndexKey: URIs can be duplicated, no stable ID
          <div key={index} className="flex gap-2">
            <Input
              name={name}
              value={uri}
              onChange={(e) => updateUri(index, e.target.value)}
              placeholder={t(OpenIddictKeys.ClientsEdit.UriPlaceholder)}
            />
            <Button type="button" variant="danger" size="sm" onClick={() => removeUri(index)}>
              {t(OpenIddictKeys.ClientsEdit.UriRemoveButton)}
            </Button>
          </div>
        ))}
      </div>
      <Button type="button" variant="ghost" size="sm" className="mt-2" onClick={addUri}>
        {t(OpenIddictKeys.ClientsEdit.UriAddButton)}
      </Button>
    </Field>
  );
}

export default function ClientsEdit({
  client,
  redirectUris,
  postLogoutUris,
  permissions,
  tab,
}: Props) {
  const { t } = useTranslation('OpenIddict');
  const [selectedPermissions, setSelectedPermissions] = useState<Set<string>>(new Set(permissions));

  function togglePermission(perm: string) {
    setSelectedPermissions((prev) => {
      const next = new Set(prev);
      if (next.has(perm)) {
        next.delete(perm);
      } else {
        next.add(perm);
      }
      return next;
    });
  }

  return (
    <Container className="space-y-4 sm:space-y-6">
      <Breadcrumb>
        <BreadcrumbList>
          <BreadcrumbItem>
            <BreadcrumbLink href="/openiddict/clients">
              {t(OpenIddictKeys.ClientsEdit.Breadcrumb)}
            </BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbPage>{t(OpenIddictKeys.ClientsEdit.BreadcrumbPage)}</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>
      <h1 className="text-2xl font-bold tracking-tight">{t(OpenIddictKeys.ClientsEdit.Title)}</h1>

      <Tabs
        value={tab}
        onValueChange={(value) =>
          router.get(
            `/openiddict/clients/${client.id}/edit`,
            { tab: value },
            { preserveState: true },
          )
        }
        className="mb-6"
      >
        <TabsList>
          {tabIds.map((tabDef) => (
            <TabsTrigger key={tabDef.id} value={tabDef.id}>
              {t(OpenIddictKeys.ClientsEdit[tabDef.key])}
            </TabsTrigger>
          ))}
        </TabsList>
      </Tabs>

      {tab === 'details' && (
        <Card>
          <CardContent className="p-4 sm:p-6">
            <form
              onSubmit={(e) => {
                e.preventDefault();
                router.post(`/openiddict/clients/${client.id}`, new FormData(e.currentTarget));
              }}
            >
              <FieldGroup>
                <Field>
                  <Label htmlFor="displayName">
                    {t(OpenIddictKeys.ClientsEdit.DisplayNameLabel)}
                  </Label>
                  <Input
                    id="displayName"
                    name="displayName"
                    defaultValue={client.displayName ?? ''}
                  />
                </Field>
                <Field>
                  <Label htmlFor="clientType">
                    {t(OpenIddictKeys.ClientsEdit.ClientTypeLabel)}
                  </Label>
                  <Select defaultValue={client.clientType ?? 'public'} name="clientType">
                    <SelectTrigger id="clientType">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="public">
                        {t(OpenIddictKeys.ClientsEdit.ClientTypePublic)}
                      </SelectItem>
                      <SelectItem value="confidential">
                        {t(OpenIddictKeys.ClientsEdit.ClientTypeConfidential)}
                      </SelectItem>
                    </SelectContent>
                  </Select>
                </Field>
                <Button type="submit" name="Save">
                  Save
                </Button>
              </FieldGroup>
            </form>
          </CardContent>
        </Card>
      )}

      {tab === 'uris' && (
        <Card>
          <CardHeader>
            <CardTitle>{t(OpenIddictKeys.ClientsEdit.RedirectUrisTitle)}</CardTitle>
          </CardHeader>
          <CardContent className="p-4 sm:p-6">
            <form
              onSubmit={(e) => {
                e.preventDefault();
                router.post(`/openiddict/clients/${client.id}/uris`, new FormData(e.currentTarget));
              }}
            >
              <FieldGroup className="space-y-4 sm:space-y-6">
                <UriList
                  label={t(OpenIddictKeys.ClientsEdit.RedirectUrisLabel)}
                  name="redirectUris"
                  values={redirectUris}
                />
                <UriList
                  label={t(OpenIddictKeys.ClientsEdit.PostLogoutUrisLabel)}
                  name="postLogoutUris"
                  values={postLogoutUris}
                />
                <Button type="submit">{t(OpenIddictKeys.ClientsEdit.SaveUrisButton)}</Button>
              </FieldGroup>
            </form>
          </CardContent>
        </Card>
      )}

      {tab === 'permissions' && (
        <Card>
          <CardHeader>
            <CardTitle>{t(OpenIddictKeys.ClientsEdit.PermissionsTitle)}</CardTitle>
          </CardHeader>
          <CardContent className="p-4 sm:p-6">
            <form
              onSubmit={(e) => {
                e.preventDefault();
                router.post(
                  `/openiddict/clients/${client.id}/permissions`,
                  new FormData(e.currentTarget),
                );
              }}
            >
              <div className="space-y-4 sm:space-y-6">
                {permissionGroupDefs.map((group) => (
                  <div key={group.key}>
                    <h3 className="text-sm font-semibold mb-2">
                      {t(OpenIddictKeys.ClientsEdit.PermGroup[group.key])}
                    </h3>
                    <div className="space-y-2">
                      {group.permissions.map((perm) => (
                        <div key={perm.value} className="flex items-center gap-2">
                          <Checkbox
                            id={perm.value}
                            name="permissions"
                            value={perm.value}
                            checked={selectedPermissions.has(perm.value)}
                            onCheckedChange={() => togglePermission(perm.value)}
                          />
                          <Label htmlFor={perm.value} className="text-sm font-normal">
                            {t(OpenIddictKeys.ClientsEdit.Perm[perm.key])}{' '}
                            <span className="text-text-muted font-mono text-xs">
                              ({perm.value})
                            </span>
                          </Label>
                        </div>
                      ))}
                    </div>
                  </div>
                ))}
              </div>
              <Button type="submit" className="mt-4">
                {t(OpenIddictKeys.ClientsEdit.SavePermissionsButton)}
              </Button>
            </form>
          </CardContent>
        </Card>
      )}
    </Container>
  );
}
