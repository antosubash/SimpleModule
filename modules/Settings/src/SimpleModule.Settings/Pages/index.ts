export const pages: Record<string, unknown> = {
  'Settings/AdminSettings': () => import('./AdminSettings'),
  'Settings/UserSettings': () => import('./UserSettings'),
  'Settings/MenuManager': () => import('./MenuManager'),
};
