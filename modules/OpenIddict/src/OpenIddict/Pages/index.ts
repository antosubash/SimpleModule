export const pages: Record<string, any> = {
  'OpenIddictModule/OpenIddict/Clients': () => import('./OpenIddict/Clients'),
  'OpenIddictModule/OpenIddict/ClientsCreate': () => import('./OpenIddict/ClientsCreate'),
  'OpenIddictModule/OpenIddict/ClientsEdit': () => import('./OpenIddict/ClientsEdit'),
};
