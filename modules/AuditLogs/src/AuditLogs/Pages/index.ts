export const pages: Record<string, unknown> = {
  'AuditLogs/Browse': () => import('../Views/Browse'),
  'AuditLogs/Dashboard': () => import('../Views/Dashboard'),
  'AuditLogs/Detail': () => import('../Views/Detail'),
};
