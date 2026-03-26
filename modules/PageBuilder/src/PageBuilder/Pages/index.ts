export const pages: Record<string, unknown> = {
  'PageBuilder/Manage': () => import('../Views/Manage'),
  'PageBuilder/Editor': () => import('../Views/Editor'),
  'PageBuilder/Viewer': () => import('../Views/Viewer'),
  'PageBuilder/PagesList': () => import('../Views/PagesList'),
};
