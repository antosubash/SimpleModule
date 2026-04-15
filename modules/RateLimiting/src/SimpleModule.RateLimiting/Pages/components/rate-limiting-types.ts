export interface RateLimitRule {
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

export interface ActivePolicy {
  name: string;
  policyType: string;
  target: string;
  permitLimit: number;
  windowSeconds: number;
  tokenLimit: number;
  tokensPerPeriod: number;
}

export const POLICY_TYPES = ['FixedWindow', 'SlidingWindow', 'TokenBucket'];
export const TARGETS = ['Ip', 'User', 'IpAndUser', 'Global'];
export const API_BASE = '/api/rate-limiting';
