export const pages: Record<string, any> = {
  'AuditLogs/Browse': () => import('../Views/Browse'),
  'AuditLogs/Dashboard': () => import('../Views/Dashboard'),
  'AuditLogs/Detail': () => import('../Views/Detail'),
};
