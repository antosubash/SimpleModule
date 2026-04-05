export const pages: Record<string, unknown> = {
  'BackgroundJobs/Dashboard': () => import('./Dashboard'),
  'BackgroundJobs/List': () => import('./List'),
  'BackgroundJobs/Detail': () => import('./Detail'),
  'BackgroundJobs/Recurring': () => import('./Recurring'),
};
