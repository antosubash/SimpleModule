import { router } from '@inertiajs/react';
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
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
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from '@simplemodule/ui';
import { useState } from 'react';

interface RateLimitRule {
  id: number;
  policyName: string;
  policyType: string;
  target: string;
  permitLimit: number;
  windowSeconds: number;
  segmentsPerWindow: number;
  tokenLimit: number;
  tokensPerPeriod: number;
  replenishmentPeriodSeconds: number;
  queueLimit: number;
  endpointPattern: string | null;
  isEnabled: boolean;
  createdAt: string;
  updatedAt: string | null;
}

interface ActivePolicy {
  name: string;
  policyType: string;
  target: string;
  permitLimit: number;
  windowSeconds: number;
  tokenLimit: number;
  tokensPerPeriod: number;
}

interface AdminProps {
  rules: RateLimitRule[];
  activePolicies: ActivePolicy[];
}

const policyTypes = ['FixedWindow', 'SlidingWindow', 'TokenBucket'];
const targets = ['Ip', 'User', 'IpAndUser', 'Global'];
const API_BASE = '/api/rate-limiting';

function PolicyTypeBadge({ type }: { type: string }) {
  const variant =
    type === 'TokenBucket' ? 'secondary' : type === 'SlidingWindow' ? 'outline' : 'default';
  return <Badge variant={variant}>{type}</Badge>;
}

function TargetBadge({ target }: { target: string }) {
  return <Badge variant="outline">{target}</Badge>;
}

export default function Admin({ rules, activePolicies }: AdminProps) {
  const [showCreateDialog, setShowCreateDialog] = useState(false);
  const [formData, setFormData] = useState({
    policyName: '',
    policyType: 'FixedWindow',
    target: 'Ip',
    permitLimit: 60,
    windowSeconds: 60,
    segmentsPerWindow: 4,
    tokenLimit: 100,
    tokensPerPeriod: 10,
    replenishmentPeriodSeconds: 10,
    queueLimit: 0,
    endpointPattern: '',
    isEnabled: true,
  });

  const handleCreate = async () => {
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
                <Dialog open={showCreateDialog} onOpenChange={setShowCreateDialog}>
                  <DialogTrigger asChild>
                    <Button>Create Rule</Button>
                  </DialogTrigger>
                  <DialogContent className="max-w-lg">
                    <DialogHeader>
                      <DialogTitle>Create Rate Limit Rule</DialogTitle>
                    </DialogHeader>
                    <div className="grid gap-4 py-4">
                      <div className="grid gap-2">
                        <Label htmlFor="policyName">Policy Name</Label>
                        <Input
                          id="policyName"
                          value={formData.policyName}
                          onChange={(e) =>
                            setFormData((prev) => ({ ...prev, policyName: e.target.value }))
                          }
                          placeholder="e.g., api-default"
                        />
                      </div>
                      <div className="grid grid-cols-2 gap-4">
                        <div className="grid gap-2">
                          <Label>Policy Type</Label>
                          <Select
                            value={formData.policyType}
                            onValueChange={(v) =>
                              setFormData((prev) => ({ ...prev, policyType: v }))
                            }
                          >
                            <SelectTrigger>
                              <SelectValue />
                            </SelectTrigger>
                            <SelectContent>
                              {policyTypes.map((t) => (
                                <SelectItem key={t} value={t}>
                                  {t}
                                </SelectItem>
                              ))}
                            </SelectContent>
                          </Select>
                        </div>
                        <div className="grid gap-2">
                          <Label>Target</Label>
                          <Select
                            value={formData.target}
                            onValueChange={(v) => setFormData((prev) => ({ ...prev, target: v }))}
                          >
                            <SelectTrigger>
                              <SelectValue />
                            </SelectTrigger>
                            <SelectContent>
                              {targets.map((t) => (
                                <SelectItem key={t} value={t}>
                                  {t}
                                </SelectItem>
                              ))}
                            </SelectContent>
                          </Select>
                        </div>
                      </div>
                      {formData.policyType !== 'TokenBucket' && (
                        <div className="grid grid-cols-2 gap-4">
                          <div className="grid gap-2">
                            <Label htmlFor="permitLimit">Permit Limit</Label>
                            <Input
                              id="permitLimit"
                              type="number"
                              value={formData.permitLimit}
                              onChange={(e) =>
                                setFormData((prev) => ({
                                  ...prev,
                                  permitLimit: Number.parseInt(e.target.value, 10),
                                }))
                              }
                            />
                          </div>
                          <div className="grid gap-2">
                            <Label htmlFor="windowSeconds">Window (seconds)</Label>
                            <Input
                              id="windowSeconds"
                              type="number"
                              value={formData.windowSeconds}
                              onChange={(e) =>
                                setFormData((prev) => ({
                                  ...prev,
                                  windowSeconds: Number.parseInt(e.target.value, 10),
                                }))
                              }
                            />
                          </div>
                        </div>
                      )}
                      {formData.policyType === 'TokenBucket' && (
                        <div className="grid grid-cols-2 gap-4">
                          <div className="grid gap-2">
                            <Label htmlFor="tokenLimit">Token Limit</Label>
                            <Input
                              id="tokenLimit"
                              type="number"
                              value={formData.tokenLimit}
                              onChange={(e) =>
                                setFormData((prev) => ({
                                  ...prev,
                                  tokenLimit: Number.parseInt(e.target.value, 10),
                                }))
                              }
                            />
                          </div>
                          <div className="grid gap-2">
                            <Label htmlFor="tokensPerPeriod">Tokens Per Period</Label>
                            <Input
                              id="tokensPerPeriod"
                              type="number"
                              value={formData.tokensPerPeriod}
                              onChange={(e) =>
                                setFormData((prev) => ({
                                  ...prev,
                                  tokensPerPeriod: Number.parseInt(e.target.value, 10),
                                }))
                              }
                            />
                          </div>
                        </div>
                      )}
                      <div className="grid gap-2">
                        <Label htmlFor="endpointPattern">Endpoint Pattern (optional)</Label>
                        <Input
                          id="endpointPattern"
                          value={formData.endpointPattern}
                          onChange={(e) =>
                            setFormData((prev) => ({ ...prev, endpointPattern: e.target.value }))
                          }
                          placeholder="e.g., /api/products/*"
                        />
                      </div>
                      <Button onClick={handleCreate}>Create</Button>
                    </div>
                  </DialogContent>
                </Dialog>
              </div>
            </CardHeader>
            <CardContent>
              {rules.length === 0 ? (
                <p className="text-sm text-muted-foreground py-8 text-center">
                  No rate limit rules configured yet.
                </p>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Policy Name</TableHead>
                      <TableHead>Type</TableHead>
                      <TableHead>Target</TableHead>
                      <TableHead>Limit</TableHead>
                      <TableHead>Endpoint</TableHead>
                      <TableHead>Enabled</TableHead>
                      <TableHead />
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {rules.map((rule) => (
                      <TableRow key={rule.id}>
                        <TableCell className="font-medium">{rule.policyName}</TableCell>
                        <TableCell>
                          <PolicyTypeBadge type={rule.policyType} />
                        </TableCell>
                        <TableCell>
                          <TargetBadge target={rule.target} />
                        </TableCell>
                        <TableCell>
                          {rule.policyType === 'TokenBucket'
                            ? `${rule.tokenLimit} tokens`
                            : `${rule.permitLimit} req/${rule.windowSeconds}s`}
                        </TableCell>
                        <TableCell className="text-muted-foreground text-sm">
                          {rule.endpointPattern ?? 'All'}
                        </TableCell>
                        <TableCell>
                          <Switch
                            checked={rule.isEnabled}
                            onCheckedChange={() => handleToggle(rule)}
                          />
                        </TableCell>
                        <TableCell>
                          <Button variant="ghost" size="sm" onClick={() => handleDelete(rule.id)}>
                            Delete
                          </Button>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
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
              {activePolicies.length === 0 ? (
                <p className="text-sm text-muted-foreground py-8 text-center">
                  No active rate limit policies.
                </p>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Name</TableHead>
                      <TableHead>Type</TableHead>
                      <TableHead>Target</TableHead>
                      <TableHead>Limit</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {activePolicies.map((policy) => (
                      <TableRow key={policy.name}>
                        <TableCell className="font-medium font-mono text-sm">
                          {policy.name}
                        </TableCell>
                        <TableCell>
                          <PolicyTypeBadge type={policy.policyType} />
                        </TableCell>
                        <TableCell>
                          <TargetBadge target={policy.target} />
                        </TableCell>
                        <TableCell>
                          {policy.policyType === 'TokenBucket'
                            ? `${policy.tokenLimit} tokens (${policy.tokensPerPeriod}/period)`
                            : `${policy.permitLimit} req/${policy.windowSeconds}s`}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </PageShell>
  );
}
