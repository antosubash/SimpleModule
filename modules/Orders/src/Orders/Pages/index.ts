export const pages: Record<string, any> = {
  'Orders/List': () => import('./List'),
  'Orders/Create': () => import('./Create'),
  'Orders/Edit': () => import('./Edit'),
};
