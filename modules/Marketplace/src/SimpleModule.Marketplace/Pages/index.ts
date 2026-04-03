export const pages: Record<string, unknown> = {
  'Marketplace/Browse': () => import('@/Views/Browse'),
  'Marketplace/Detail': () => import('@/Views/Detail'),
};
