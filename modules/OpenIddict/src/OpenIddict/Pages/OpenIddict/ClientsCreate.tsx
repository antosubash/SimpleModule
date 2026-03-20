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
  Input,
  Label,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@simplemodule/ui';
import { useState } from 'react';

export default function ClientsCreate() {
  const [clientType, setClientType] = useState('public');

  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    router.post('/openiddict/clients', new FormData(e.currentTarget));
  }

  return (
    <div className="max-w-xl">
      <Breadcrumb className="mb-4">
        <BreadcrumbList>
          <BreadcrumbItem>
            <BreadcrumbLink href="/openiddict/clients">Clients</BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbPage>Create Client</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>
      <h1 className="text-2xl font-bold tracking-tight mb-6">Create Client</h1>

      <Card>
        <CardContent className="p-6">
          <form onSubmit={handleSubmit} className="space-y-6">
            <div>
              <Label htmlFor="clientId">Client ID</Label>
              <Input id="clientId" name="clientId" required placeholder="my-app-client" />
            </div>
            <div>
              <Label htmlFor="displayName">Display Name</Label>
              <Input id="displayName" name="displayName" placeholder="My Application" />
            </div>
            <div>
              <Label htmlFor="clientType">Client Type</Label>
              <Select value={clientType} onValueChange={setClientType} name="clientType">
                <SelectTrigger id="clientType">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="public">Public</SelectItem>
                  <SelectItem value="confidential">Confidential</SelectItem>
                </SelectContent>
              </Select>
            </div>
            {clientType === 'confidential' && (
              <div>
                <Label htmlFor="clientSecret">Client Secret</Label>
                <Input
                  id="clientSecret"
                  name="clientSecret"
                  type="password"
                  placeholder="Enter a strong secret"
                />
              </div>
            )}
            <Button type="submit">Create Client</Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
