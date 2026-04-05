export const pages: Record<string, unknown> = {
  'Marketplace/Browse': () => import('./Browse'),
  'Marketplace/Detail': () => import('./Detail'),
};
