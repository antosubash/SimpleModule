import {
  Button,
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
  Input,
  Label,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@simplemodule/ui';
import { useState } from 'react';
import { POLICY_TYPES, TARGETS } from './rate-limiting-types';

interface FormData {
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
  endpointPattern: string;
  isEnabled: boolean;
}

const defaultFormData: FormData = {
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
};

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onCreate: (data: FormData) => void;
}

export function CreateRuleDialog({ open, onOpenChange, onCreate }: Props) {
  const [formData, setFormData] = useState<FormData>(defaultFormData);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
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
              onChange={(e) => setFormData((prev) => ({ ...prev, policyName: e.target.value }))}
              placeholder="e.g., api-default"
            />
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div className="grid gap-2">
              <Label>Policy Type</Label>
              <Select
                value={formData.policyType}
                onValueChange={(v) => setFormData((prev) => ({ ...prev, policyType: v }))}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {POLICY_TYPES.map((t) => (
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
                  {TARGETS.map((t) => (
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
          <Button onClick={() => onCreate(formData)}>Create</Button>
        </div>
      </DialogContent>
    </Dialog>
  );
}
