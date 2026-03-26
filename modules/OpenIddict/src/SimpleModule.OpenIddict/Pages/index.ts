export const pages: Record<string, unknown> = {
  'OpenIddict/OpenIddict/Clients': () => import('./OpenIddict/Clients'),
  'OpenIddict/OpenIddict/ClientsCreate': () => import('./OpenIddict/ClientsCreate'),
  'OpenIddict/OpenIddict/ClientsEdit': () => import('./OpenIddict/ClientsEdit'),
};
