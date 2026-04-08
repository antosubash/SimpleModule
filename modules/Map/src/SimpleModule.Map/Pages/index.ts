export const pages: Record<string, unknown> = {
  'Map/Browse': () => import('./Browse'),
  'Map/Layers': () => import('./Layers'),
  'Map/View': () => import('./View'),
  'Map/Edit': () => import('./Edit'),
};
