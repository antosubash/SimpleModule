export const OVERRIDE_TYPE_USER = 0 as const;
export const OVERRIDE_TYPE_ROLE = 1 as const;
export type OverrideType = typeof OVERRIDE_TYPE_USER | typeof OVERRIDE_TYPE_ROLE;
