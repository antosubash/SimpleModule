export const pages: Record<string, any> = {
  'PageBuilder/Manage': () => import('../Views/Manage'),
  'PageBuilder/Editor': () => import('../Views/Editor'),
  'PageBuilder/Viewer': () => import('../Views/Viewer'),
  'PageBuilder/PagesList': () => import('../Views/PagesList'),
};
