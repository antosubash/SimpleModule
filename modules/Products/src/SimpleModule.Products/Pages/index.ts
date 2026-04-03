export const pages: Record<string, unknown> = {
  'Products/Browse': () => import('@/Views/Browse'),
  'Products/Manage': () => import('@/Views/Manage'),
  'Products/Create': () => import('@/Views/Create'),
  'Products/Edit': () => import('@/Views/Edit'),
};
