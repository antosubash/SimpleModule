export const pages: Record<string, unknown> = {
  'Map/Browse': () => import('./Browse'),
  'Map/Layers': () => import('./Layers'),
};
