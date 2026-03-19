import { router } from '@inertiajs/react';
import {
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Checkbox,
  Input,
  Label,
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

interface Tab {
  id: string;
  label: string;
}

const tabs: Tab[] = [
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

function TabNav({ activeTab, baseUrl }: { activeTab: string; baseUrl: string }) {
  return (
    <div className="border-b border-border mb-6">
      <nav className="flex gap-0 -mb-px">
        {tabs.map((tab) => (
          <button
            key={tab.id}
            type="button"
            onClick={() => router.get(baseUrl, { tab: tab.id }, { preserveState: true })}
            className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
              activeTab === tab.id
                ? 'border-primary text-primary'
                : 'border-transparent text-text-muted hover:text-text hover:border-border'
            }`}
          >
            {tab.label}
          </button>
        ))}
      </nav>
    </div>
  );
}

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
    <div>
      <Label>{label}</Label>
      <div className="space-y-2 mt-2">
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
    </div>
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
    <div className="max-w-3xl">
      <div className="flex items-center gap-3 mb-1">
        <a
          href="/openiddict/clients"
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
        <h1 className="text-2xl font-extrabold tracking-tight">Edit Client</h1>
      </div>
      <p className="text-text-muted text-sm ml-7 mb-6">
        <span className="font-mono">{client.clientId}</span>
      </p>

      <TabNav activeTab={tab} baseUrl={`/openiddict/clients/${client.id}/edit`} />

      {tab === 'details' && (
        <Card>
          <CardContent className="p-6">
            <form
              onSubmit={(e) => {
                e.preventDefault();
                router.post(`/openiddict/clients/${client.id}`, new FormData(e.currentTarget));
              }}
              className="space-y-4"
            >
              <div>
                <Label htmlFor="displayName">Display Name</Label>
                <Input
                  id="displayName"
                  name="displayName"
                  defaultValue={client.displayName ?? ''}
                />
              </div>
              <div>
                <Label htmlFor="clientType">Client Type</Label>
                <select
                  id="clientType"
                  name="clientType"
                  defaultValue={client.clientType ?? 'public'}
                  className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                >
                  <option value="public">Public</option>
                  <option value="confidential">Confidential</option>
                </select>
              </div>
              <Button type="submit">Save</Button>
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
              className="space-y-6"
            >
              <UriList label="Redirect URIs" name="redirectUris" values={redirectUris} />
              <UriList
                label="Post-Logout Redirect URIs"
                name="postLogoutUris"
                values={postLogoutUris}
              />
              <Button type="submit">Save URIs</Button>
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
