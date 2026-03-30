import {
  Badge,
  Button,
  Card,
  CardContent,
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
  Field,
  FieldGroup,
  Input,
  Label,
  PageShell,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Switch,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';
import { useState } from 'react';
import { OVERRIDE_TYPE_ROLE, OVERRIDE_TYPE_USER } from '../constants';
import type { FeatureFlag, FeatureFlagOverride } from '../types';

interface ManageProps {
  flags: FeatureFlag[];
}

export default function Manage({ flags: initialFlags }: ManageProps) {
  const [flags, setFlags] = useState(initialFlags);
  const [selectedFlag, setSelectedFlag] = useState<string | null>(null);
  const [overrides, setOverrides] = useState<FeatureFlagOverride[]>([]);
  const [overrideDialogOpen, setOverrideDialogOpen] = useState(false);

  const handleToggle = async (name: string, enabled: boolean) => {
    const response = await fetch(`/api/feature-flags/${encodeURIComponent(name)}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ isEnabled: enabled }),
    });
    if (response.ok) {
      setFlags((prev) => prev.map((f) => (f.name === name ? { ...f, isEnabled: enabled } : f)));
    }
  };

  const loadOverrides = async (name: string) => {
    const response = await fetch(`/api/feature-flags/${encodeURIComponent(name)}/overrides`);
    if (response.ok) {
      const data = await response.json();
      setOverrides(data);
      setSelectedFlag(name);
      setOverrideDialogOpen(true);
    }
  };

  const handleAddOverride = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!selectedFlag) return;

    const formData = new FormData(e.currentTarget);
    const overrideType = Number(formData.get('overrideType'));
    const overrideValue = formData.get('overrideValue') as string;
    const isEnabled = formData.get('isEnabled') === 'on';

    const response = await fetch(
      `/api/feature-flags/${encodeURIComponent(selectedFlag)}/overrides`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ overrideType, overrideValue, isEnabled }),
      },
    );

    if (response.ok) {
      const newOverride: FeatureFlagOverride = await response.json();
      setOverrides((prev) => [...prev, newOverride]);
      e.currentTarget.reset();
    }
  };

  const handleDeleteOverride = async (id: number) => {
    const response = await fetch(`/api/feature-flags/overrides/${id}`, { method: 'DELETE' });
    if (response.ok && selectedFlag) {
      setOverrides((prev) => prev.filter((o) => o.id !== id));
    }
  };

  const activeFlags = flags.filter((f) => !f.isDeprecated);
  const deprecatedFlags = flags.filter((f) => f.isDeprecated);

  return (
    <PageShell title="Feature Flags" description="Manage feature flags across all modules.">
      <Card>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>Description</TableHead>
                <TableHead>Default</TableHead>
                <TableHead>Enabled</TableHead>
                <TableHead>Overrides</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {activeFlags.map((flag) => (
                <TableRow key={flag.name}>
                  <TableCell className="font-mono text-sm">{flag.name}</TableCell>
                  <TableCell className="text-text-muted">{flag.description}</TableCell>
                  <TableCell>
                    <Badge variant={flag.defaultEnabled ? 'default' : 'secondary'}>
                      {flag.defaultEnabled ? 'on' : 'off'}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    <Switch
                      checked={flag.isEnabled}
                      onCheckedChange={(checked) => handleToggle(flag.name, checked)}
                    />
                  </TableCell>
                  <TableCell>
                    <Button variant="outline" size="sm" onClick={() => loadOverrides(flag.name)}>
                      Overrides
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {deprecatedFlags.length > 0 && (
        <Card className="mt-6">
          <CardContent>
            <h3 className="text-lg font-semibold mb-4">Deprecated Flags</h3>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Enabled</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {deprecatedFlags.map((flag) => (
                  <TableRow key={flag.name} className="opacity-60">
                    <TableCell className="font-mono text-sm">
                      {flag.name}{' '}
                      <Badge variant="destructive" className="ml-2">
                        deprecated
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <Switch
                        checked={flag.isEnabled}
                        onCheckedChange={(checked) => handleToggle(flag.name, checked)}
                      />
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      )}

      <Dialog open={overrideDialogOpen} onOpenChange={setOverrideDialogOpen}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>Overrides for {selectedFlag}</DialogTitle>
          </DialogHeader>

          {overrides.length > 0 && (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Type</TableHead>
                  <TableHead>Value</TableHead>
                  <TableHead>Enabled</TableHead>
                  <TableHead />
                </TableRow>
              </TableHeader>
              <TableBody>
                {overrides.map((o) => (
                  <TableRow key={o.id}>
                    <TableCell>
                      <Badge>{o.overrideType === OVERRIDE_TYPE_USER ? 'User' : 'Role'}</Badge>
                    </TableCell>
                    <TableCell className="font-mono text-sm">{o.overrideValue}</TableCell>
                    <TableCell>{o.isEnabled ? 'Yes' : 'No'}</TableCell>
                    <TableCell>
                      <Button
                        variant="destructive"
                        size="sm"
                        onClick={() => handleDeleteOverride(o.id)}
                      >
                        Delete
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}

          <form onSubmit={handleAddOverride} className="space-y-4 mt-4">
            <FieldGroup>
              <Field>
                <Label htmlFor="overrideType">Type</Label>
                <Select defaultValue={String(OVERRIDE_TYPE_USER)} name="overrideType">
                  <SelectTrigger id="overrideType">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value={String(OVERRIDE_TYPE_USER)}>User</SelectItem>
                    <SelectItem value={String(OVERRIDE_TYPE_ROLE)}>Role</SelectItem>
                  </SelectContent>
                </Select>
              </Field>
              <Field>
                <Label htmlFor="overrideValue">Value (User ID or Role Name)</Label>
                <Input id="overrideValue" name="overrideValue" required />
              </Field>
              <Field className="flex items-center gap-2">
                <Label htmlFor="isEnabled">Enabled</Label>
                <input type="checkbox" id="isEnabled" name="isEnabled" defaultChecked />
              </Field>
            </FieldGroup>
            <Button type="submit">Add Override</Button>
          </form>
        </DialogContent>
      </Dialog>
    </PageShell>
  );
}
