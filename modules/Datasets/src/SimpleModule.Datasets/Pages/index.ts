export const pages: Record<string, unknown> = {
  'Datasets/Browse': () => import('./Browse'),
  'Datasets/Upload': () => import('./Upload'),
  'Datasets/Detail': () => import('./Detail'),
};
