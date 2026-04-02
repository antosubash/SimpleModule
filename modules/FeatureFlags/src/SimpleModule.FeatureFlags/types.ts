// Auto-generated from [Dto] types — do not edit
export interface FeatureFlag {
  name: string;
  description: string;
  isEnabled: boolean;
  defaultEnabled: boolean;
  isDeprecated: boolean;
  updatedAt: string;
}

export interface FeatureFlagOverride {
  id: number;
  flagName: string;
  overrideType: any;
  overrideValue: string;
  isEnabled: boolean;
}

export interface SetOverrideRequest {
  overrideType: any;
  overrideValue: string;
  isEnabled: boolean;
}

export interface UpdateFeatureFlagRequest {
  isEnabled: boolean;
}

