export const pages: Record<string, any> = {
  'Products/Browse': () => import('../Views/Browse'),
  'Products/Manage': () => import('../Views/Manage'),
  'Products/Create': () => import('../Views/Create'),
  'Products/Edit': () => import('../Views/Edit'),
};
