import {
  Button,
  Switch,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';
import { PolicyTypeBadge, TargetBadge } from './PolicyBadges';
import type { RateLimitRule } from './rate-limiting-types';

interface Props {
  rules: RateLimitRule[];
  onToggle: (rule: RateLimitRule) => void;
  onDelete: (id: number) => void;
}

export function RulesTable({ rules, onToggle, onDelete }: Props) {
  if (rules.length === 0) {
    return (
      <p className="text-sm text-muted-foreground py-8 text-center">
        No rate limit rules configured yet.
      </p>
    );
  }

  return (
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
              <Switch checked={rule.isEnabled} onCheckedChange={() => onToggle(rule)} />
            </TableCell>
            <TableCell>
              <Button variant="ghost" size="sm" onClick={() => onDelete(rule.id)}>
                Delete
              </Button>
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}
