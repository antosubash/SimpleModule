export const pages: Record<string, unknown> = {
  'Products/Browse': () => import('./Browse'),
  'Products/Manage': () => import('./Manage'),
  'Products/Create': () => import('./Create'),
  'Products/Edit': () => import('./Edit'),
};
