export const pages: Record<string, unknown> = {
  'BackgroundJobs/Dashboard': () => import('./Views/Dashboard'),
  'BackgroundJobs/List': () => import('./Views/List'),
  'BackgroundJobs/Detail': () => import('./Views/Detail'),
  'BackgroundJobs/Recurring': () => import('./Views/Recurring'),
};
