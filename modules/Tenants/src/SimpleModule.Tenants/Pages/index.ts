export const pages: Record<string, unknown> = {
  'Tenants/Browse': () => import('./Browse'),
  'Tenants/Manage': () => import('./Manage'),
  'Tenants/Create': () => import('./Create'),
  'Tenants/Edit': () => import('./Edit'),
  'Tenants/Features': () => import('./Features'),
};
