export const pages: Record<string, unknown> = {
  'OpenIddictModule/OpenIddict/Clients': () => import('./OpenIddict/Clients'),
  'OpenIddictModule/OpenIddict/ClientsCreate': () => import('./OpenIddict/ClientsCreate'),
  'OpenIddictModule/OpenIddict/ClientsEdit': () => import('./OpenIddict/ClientsEdit'),
};
