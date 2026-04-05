export const pages: Record<string, unknown> = {
  'Email/Templates': () => import('./Templates'),
  'Email/CreateTemplate': () => import('./CreateTemplate'),
  'Email/EditTemplate': () => import('./EditTemplate'),
  'Email/History': () => import('./History'),
  'Email/Dashboard': () => import('./Dashboard'),
};
