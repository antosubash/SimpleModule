export const pages: Record<string, unknown> = {
  'Tenants/Browse': () => import('../Views/Browse'),
  'Tenants/Manage': () => import('../Views/Manage'),
  'Tenants/Create': () => import('../Views/Create'),
  'Tenants/Edit': () => import('../Views/Edit'),
  'Tenants/Features': () => import('../Views/Features'),
};
