import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Badge,
  Button,
  Card,
  CardContent,
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
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
import { FeatureFlagsKeys } from '../Locales/keys';
import type { FeatureFlag, FeatureFlagOverride } from '../types';

interface ManageProps {
  flags: FeatureFlag[];
}

export default function Manage({ flags: initialFlags }: ManageProps) {
  const { t } = useTranslation('FeatureFlags');
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
    <PageShell
      title={t(FeatureFlagsKeys.Manage.Title)}
      description={t(FeatureFlagsKeys.Manage.Description)}
    >
      <Card>
        <CardContent className="p-4 sm:p-6">
          <div className="overflow-x-auto -mx-4 px-4 sm:mx-0 sm:px-0">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t(FeatureFlagsKeys.Manage.ColName)}</TableHead>
                  <TableHead>{t(FeatureFlagsKeys.Manage.ColDescription)}</TableHead>
                  <TableHead>{t(FeatureFlagsKeys.Manage.ColDefault)}</TableHead>
                  <TableHead>{t(FeatureFlagsKeys.Manage.ColEnabled)}</TableHead>
                  <TableHead>{t(FeatureFlagsKeys.Manage.ColOverrides)}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {activeFlags.map((flag) => (
                  <TableRow key={flag.name}>
                    <TableCell className="font-mono text-sm">{flag.name}</TableCell>
                    <TableCell className="text-text-muted">{flag.description}</TableCell>
                    <TableCell>
                      <Badge variant={flag.defaultEnabled ? 'info' : 'default'}>
                        {flag.defaultEnabled
                          ? t(FeatureFlagsKeys.Manage.DefaultOn)
                          : t(FeatureFlagsKeys.Manage.DefaultOff)}
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
                        {t(FeatureFlagsKeys.Manage.OverridesButton)}
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        </CardContent>
      </Card>

      {deprecatedFlags.length > 0 && (
        <Card className="mt-4 sm:mt-6">
          <CardContent className="p-4 sm:p-6">
            <h3 className="text-lg font-semibold mb-4">
              {t(FeatureFlagsKeys.Manage.DeprecatedTitle)}
            </h3>
            <div className="overflow-x-auto -mx-4 px-4 sm:mx-0 sm:px-0">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>{t(FeatureFlagsKeys.Manage.DeprecatedColName)}</TableHead>
                    <TableHead>{t(FeatureFlagsKeys.Manage.DeprecatedColEnabled)}</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {deprecatedFlags.map((flag) => (
                    <TableRow key={flag.name} className="opacity-60">
                      <TableCell className="font-mono text-sm">
                        {flag.name}{' '}
                        <Badge variant="danger" className="ml-2">
                          {t(FeatureFlagsKeys.Manage.DeprecatedBadge)}
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
            </div>
          </CardContent>
        </Card>
      )}

      <Dialog open={overrideDialogOpen} onOpenChange={setOverrideDialogOpen}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>
              {t(FeatureFlagsKeys.Manage.OverrideDialog.Title, { flagName: selectedFlag ?? '' })}
            </DialogTitle>
          </DialogHeader>

          {overrides.length > 0 && (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t(FeatureFlagsKeys.Manage.OverrideDialog.ColType)}</TableHead>
                  <TableHead>{t(FeatureFlagsKeys.Manage.OverrideDialog.ColValue)}</TableHead>
                  <TableHead>{t(FeatureFlagsKeys.Manage.OverrideDialog.ColEnabled)}</TableHead>
                  <TableHead />
                </TableRow>
              </TableHeader>
              <TableBody>
                {overrides.map((o) => (
                  <TableRow key={o.id}>
                    <TableCell>
                      <Badge>
                        {o.overrideType === OVERRIDE_TYPE_USER
                          ? t(FeatureFlagsKeys.Manage.OverrideDialog.TypeUser)
                          : t(FeatureFlagsKeys.Manage.OverrideDialog.TypeRole)}
                      </Badge>
                    </TableCell>
                    <TableCell className="font-mono text-sm">{o.overrideValue}</TableCell>
                    <TableCell>
                      {o.isEnabled
                        ? t(FeatureFlagsKeys.Manage.OverrideDialog.EnabledYes)
                        : t(FeatureFlagsKeys.Manage.OverrideDialog.EnabledNo)}
                    </TableCell>
                    <TableCell>
                      <Button variant="danger" size="sm" onClick={() => handleDeleteOverride(o.id)}>
                        {t(FeatureFlagsKeys.Manage.OverrideDialog.DeleteButton)}
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}

          <form onSubmit={handleAddOverride} className="space-y-3 sm:space-y-4 mt-4">
            <FieldGroup>
              <Field>
                <Label htmlFor="overrideType">
                  {t(FeatureFlagsKeys.Manage.OverrideDialog.TypeLabel)}
                </Label>
                <Select defaultValue={String(OVERRIDE_TYPE_USER)} name="overrideType">
                  <SelectTrigger id="overrideType">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value={String(OVERRIDE_TYPE_USER)}>
                      {t(FeatureFlagsKeys.Manage.OverrideDialog.TypeUser)}
                    </SelectItem>
                    <SelectItem value={String(OVERRIDE_TYPE_ROLE)}>
                      {t(FeatureFlagsKeys.Manage.OverrideDialog.TypeRole)}
                    </SelectItem>
                  </SelectContent>
                </Select>
              </Field>
              <Field>
                <Label htmlFor="overrideValue">
                  {t(FeatureFlagsKeys.Manage.OverrideDialog.ValueLabel)}
                </Label>
                <Input id="overrideValue" name="overrideValue" required />
              </Field>
              <Field className="flex items-center gap-2">
                <Label htmlFor="isEnabled">
                  {t(FeatureFlagsKeys.Manage.OverrideDialog.EnabledLabel)}
                </Label>
                <input type="checkbox" id="isEnabled" name="isEnabled" defaultChecked />
              </Field>
            </FieldGroup>
            <Button type="submit">{t(FeatureFlagsKeys.Manage.OverrideDialog.AddButton)}</Button>
          </form>
        </DialogContent>
      </Dialog>
    </PageShell>
  );
}
