// Auto-generated from [Dto] types — do not edit
export interface CreateRateLimitRuleRequest {
  policyName: string;
  policyType: any;
  target: any;
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

export interface RateLimitRule {
  id: number;
  policyName: string;
  policyType: any;
  target: any;
  permitLimit: number;
  windowSeconds: number;
  segmentsPerWindow: number;
  tokenLimit: number;
  tokensPerPeriod: number;
  replenishmentPeriodSeconds: number;
  queueLimit: number;
  endpointPattern: string;
  isEnabled: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface UpdateRateLimitRuleRequest {
  policyType: any;
  target: any;
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

