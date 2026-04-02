import { usePage } from '@inertiajs/react';

export function useFeatureFlag(name: string): boolean {
  const { featureFlags } = usePage<{ featureFlags?: Record<string, boolean> }>().props;
  return featureFlags?.[name] ?? false;
}
