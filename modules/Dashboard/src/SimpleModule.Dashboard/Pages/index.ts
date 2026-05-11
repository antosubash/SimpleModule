export const pages: Record<string, unknown> = {
  'Dashboard/Home': () => import('./Home'),
  'Dashboard/Broadcasting': () => import('./Broadcasting'),
};
