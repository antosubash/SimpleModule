import '@puckeditor/core/puck.css';

export const pages: Record<string, unknown> = {
  'PageBuilder/Manage': () => import('./Manage'),
  'PageBuilder/Editor': () => import('./Editor'),
  'PageBuilder/Viewer': () => import('./Viewer'),
  'PageBuilder/ViewerDraft': () => import('./Viewer'),
  'PageBuilder/PagesList': () => import('./PagesList'),
};
