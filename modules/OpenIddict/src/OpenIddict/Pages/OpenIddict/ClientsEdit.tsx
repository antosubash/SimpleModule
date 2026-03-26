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
  CardHeader,
  CardTitle,
  Checkbox,
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

const tabs = [
  { id: 'details', label: 'Details' },
  { id: 'uris', label: 'URIs' },
  { id: 'permissions', label: 'Permissions' },
];

const permissionGroups = [
  {
    label: 'Endpoints',
    permissions: [
      { value: 'ept:authorization', label: 'Authorization' },
      { value: 'ept:token', label: 'Token' },
      { value: 'ept:end_session', label: 'End Session' },
      { value: 'ept:revocation', label: 'Revocation' },
      { value: 'ept:introspection', label: 'Introspection' },
    ],
  },
  {
    label: 'Grant Types',
    permissions: [
      { value: 'gt:authorization_code', label: 'Authorization Code' },
      { value: 'gt:refresh_token', label: 'Refresh Token' },
      { value: 'gt:client_credentials', label: 'Client Credentials' },
      { value: 'gt:implicit', label: 'Implicit' },
    ],
  },
  {
    label: 'Response Types',
    permissions: [
      { value: 'rst:code', label: 'Code' },
      { value: 'rst:token', label: 'Token' },
    ],
  },
  {
    label: 'Scopes',
    permissions: [
      { value: 'scp:openid', label: 'OpenID' },
      { value: 'scp:profile', label: 'Profile' },
      { value: 'scp:email', label: 'Email' },
      { value: 'scp:roles', label: 'Roles' },
    ],
  },
];

function UriList({ label, name, values }: { label: string; name: string; values: string[] }) {
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
        {/* biome-ignore lint/suspicious/noArrayIndexKey: URIs can be duplicated, no stable ID */}
        {uris.map((uri, index) => (
          <div key={index} className="flex gap-2">
            <Input
              name={name}
              value={uri}
              onChange={(e) => updateUri(index, e.target.value)}
              placeholder="https://..."
            />
            <Button type="button" variant="danger" size="sm" onClick={() => removeUri(index)}>
              Remove
            </Button>
          </div>
        ))}
      </div>
      <Button type="button" variant="ghost" size="sm" className="mt-2" onClick={addUri}>
        + Add URI
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
    <div className="mx-auto max-w-3xl space-y-6">
      <Breadcrumb>
        <BreadcrumbList>
          <BreadcrumbItem>
            <BreadcrumbLink href="/openiddict/clients">Clients</BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbPage>Edit Client</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>
      <h1 className="text-2xl font-bold tracking-tight">Edit Client</h1>

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
          {tabs.map((t) => (
            <TabsTrigger key={t.id} value={t.id}>
              {t.label}
            </TabsTrigger>
          ))}
        </TabsList>
      </Tabs>

      {tab === 'details' && (
        <Card>
          <CardContent className="p-6">
            <form
              onSubmit={(e) => {
                e.preventDefault();
                router.post(`/openiddict/clients/${client.id}`, new FormData(e.currentTarget));
              }}
            >
              <FieldGroup>
                <Field>
                  <Label htmlFor="displayName">Display Name</Label>
                  <Input
                    id="displayName"
                    name="displayName"
                    defaultValue={client.displayName ?? ''}
                  />
                </Field>
                <Field>
                  <Label htmlFor="clientType">Client Type</Label>
                  <Select defaultValue={client.clientType ?? 'public'} name="clientType">
                    <SelectTrigger id="clientType">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="public">Public</SelectItem>
                      <SelectItem value="confidential">Confidential</SelectItem>
                    </SelectContent>
                  </Select>
                </Field>
                <Button type="submit">Save Changes</Button>
              </FieldGroup>
            </form>
          </CardContent>
        </Card>
      )}

      {tab === 'uris' && (
        <Card>
          <CardHeader>
            <CardTitle>Redirect URIs</CardTitle>
          </CardHeader>
          <CardContent>
            <form
              onSubmit={(e) => {
                e.preventDefault();
                router.post(`/openiddict/clients/${client.id}/uris`, new FormData(e.currentTarget));
              }}
            >
              <FieldGroup className="space-y-6">
                <UriList label="Redirect URIs" name="redirectUris" values={redirectUris} />
                <UriList
                  label="Post-Logout Redirect URIs"
                  name="postLogoutUris"
                  values={postLogoutUris}
                />
                <Button type="submit">Save URIs</Button>
              </FieldGroup>
            </form>
          </CardContent>
        </Card>
      )}

      {tab === 'permissions' && (
        <Card>
          <CardHeader>
            <CardTitle>Permissions</CardTitle>
          </CardHeader>
          <CardContent>
            <form
              onSubmit={(e) => {
                e.preventDefault();
                router.post(
                  `/openiddict/clients/${client.id}/permissions`,
                  new FormData(e.currentTarget),
                );
              }}
            >
              <div className="space-y-6">
                {permissionGroups.map((group) => (
                  <div key={group.label}>
                    <h3 className="text-sm font-semibold mb-2">{group.label}</h3>
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
                            {perm.label}{' '}
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
                Save Permissions
              </Button>
            </form>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
