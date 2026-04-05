export const pages: Record<string, unknown> = {
  'AuditLogs/Browse': () => import('./Browse'),
  'AuditLogs/Dashboard': () => import('./Dashboard'),
  'AuditLogs/Detail': () => import('./Detail'),
};
