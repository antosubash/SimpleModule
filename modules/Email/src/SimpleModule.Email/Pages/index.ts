export const pages: Record<string, unknown> = {
  'Email/Templates': () => import('../Views/Templates'),
  'Email/CreateTemplate': () => import('../Views/CreateTemplate'),
  'Email/EditTemplate': () => import('../Views/EditTemplate'),
  'Email/History': () => import('../Views/History'),
};
