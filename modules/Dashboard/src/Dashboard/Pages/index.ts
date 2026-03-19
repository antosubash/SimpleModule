// biome-ignore lint/suspicious/noExplicitAny: page components have varying prop types
export const pages: Record<string, any> = {
  'Dashboard/Home': () => import('./Home'),
};
