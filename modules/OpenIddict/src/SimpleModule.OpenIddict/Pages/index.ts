export const pages: Record<string, unknown> = {
  'OpenIddict/OpenIddict/Clients': () => import('./Clients'),
  'OpenIddict/OpenIddict/ClientsCreate': () => import('./ClientsCreate'),
  'OpenIddict/OpenIddict/ClientsEdit': () => import('./ClientsEdit'),
  'OpenIddict/OAuthCallback': () => import('./OAuthCallback'),
};
