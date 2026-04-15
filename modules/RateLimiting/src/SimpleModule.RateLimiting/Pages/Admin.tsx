import { router } from '@inertiajs/react';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  PageShell,
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from '@simplemodule/ui';
import { useState } from 'react';
import { ActivePoliciesTable } from './components/ActivePoliciesTable';
import { CreateRuleDialog } from './components/CreateRuleDialog';
import { RulesTable } from './components/RulesTable';
import { type ActivePolicy, API_BASE, type RateLimitRule } from './components/rate-limiting-types';

interface AdminProps {
  rules: RateLimitRule[];
  activePolicies: ActivePolicy[];
}

export default function Admin({ rules, activePolicies }: AdminProps) {
  const [showCreateDialog, setShowCreateDialog] = useState(false);

  const handleCreate = async (formData: unknown) => {
    await fetch(API_BASE, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(formData),
    });
    setShowCreateDialog(false);
    router.reload();
  };

  const handleDelete = async (id: number) => {
    await fetch(`${API_BASE}/${id}`, { method: 'DELETE' });
    router.reload();
  };

  const handleToggle = async (rule: RateLimitRule) => {
    await fetch(`${API_BASE}/${rule.id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        policyType: rule.policyType,
        target: rule.target,
        permitLimit: rule.permitLimit,
        windowSeconds: rule.windowSeconds,
        segmentsPerWindow: rule.segmentsPerWindow,
        tokenLimit: rule.tokenLimit,
        tokensPerPeriod: rule.tokensPerPeriod,
        replenishmentPeriodSeconds: rule.replenishmentPeriodSeconds,
        queueLimit: rule.queueLimit,
        endpointPattern: rule.endpointPattern,
        isEnabled: !rule.isEnabled,
      }),
    });
    router.reload();
  };

  return (
    <PageShell title="Rate Limiting">
      <Tabs defaultValue="rules">
        <TabsList>
          <TabsTrigger value="rules">Stored Rules</TabsTrigger>
          <TabsTrigger value="active">Active Policies</TabsTrigger>
        </TabsList>

        <TabsContent value="rules" className="space-y-4">
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between">
                <div>
                  <CardTitle>Rate Limit Rules</CardTitle>
                  <CardDescription>
                    Manage rate limiting policies stored in the database.
                  </CardDescription>
                </div>
                <CreateRuleDialog
                  open={showCreateDialog}
                  onOpenChange={setShowCreateDialog}
                  onCreate={handleCreate}
                />
              </div>
            </CardHeader>
            <CardContent>
              <RulesTable rules={rules} onToggle={handleToggle} onDelete={handleDelete} />
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="active" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Active Policies</CardTitle>
              <CardDescription>
                Rate limit policies currently active in the middleware pipeline. These are
                registered at application startup via module ConfigureRateLimits() hooks.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <ActivePoliciesTable policies={activePolicies} />
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </PageShell>
  );
}
