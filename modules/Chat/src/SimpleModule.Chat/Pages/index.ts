export const pages: Record<string, unknown> = {
  'Chat/Browse': () => import('./Browse'),
  'Chat/Conversation': () => import('./Conversation'),
};
