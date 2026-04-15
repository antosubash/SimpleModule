import { Badge } from '@simplemodule/ui';

export function PolicyTypeBadge({ type }: { type: string }) {
  const variant =
    type === 'TokenBucket' ? 'info' : type === 'SlidingWindow' ? 'warning' : 'default';
  return <Badge variant={variant}>{type}</Badge>;
}

export function TargetBadge({ target }: { target: string }) {
  return <Badge variant="info">{target}</Badge>;
}
