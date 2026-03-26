export const pages: Record<string, unknown> = {
  'Orders/List': () => import('./List'),
  'Orders/Create': () => import('./Create'),
  'Orders/Edit': () => import('./Edit'),
};
