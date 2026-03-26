export const pages: Record<string, unknown> = {
  'Settings/AdminSettings': () => import('../Views/AdminSettings'),
  'Settings/UserSettings': () => import('../Views/UserSettings'),
  'Settings/MenuManager': () => import('../Views/MenuManager'),
};
