import { router } from '@inertiajs/react';
import {
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
        <h1 className="text-2xl font-extrabold tracking-tight">Create Client</h1>
      </div>
      <p className="text-text-muted text-sm ml-7 mb-6">Register a new OpenID Connect client</p>

      <Card>
        <CardContent className="p-6">
          <form onSubmit={handleSubmit} className="space-y-6">
            <div>
              <Label htmlFor="clientId">Client ID</Label>
              <Input id="clientId" name="clientId" required />
            </div>
            <div>
              <Label htmlFor="displayName">Display Name</Label>
              <Input id="displayName" name="displayName" />
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
                <Input id="clientSecret" name="clientSecret" type="password" />
              </div>
            )}
            <Button type="submit">Create Client</Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
