import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@simplemodule/ui';
import { PolicyTypeBadge, TargetBadge } from './PolicyBadges';
import type { ActivePolicy } from './rate-limiting-types';

export function ActivePoliciesTable({ policies }: { policies: ActivePolicy[] }) {
  if (policies.length === 0) {
    return (
      <p className="text-sm text-muted-foreground py-8 text-center">
        No active rate limit policies.
      </p>
    );
  }

  return (
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
        {policies.map((policy) => (
          <TableRow key={policy.name}>
            <TableCell className="font-medium font-mono text-sm">{policy.name}</TableCell>
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
  );
}
