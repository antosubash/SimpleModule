export const pages: Record<string, unknown> = {
  'OpenIddict/OpenIddict/Clients': () => import('@/Views/Clients'),
  'OpenIddict/OpenIddict/ClientsCreate': () => import('@/Views/ClientsCreate'),
  'OpenIddict/OpenIddict/ClientsEdit': () => import('@/Views/ClientsEdit'),
  'OpenIddict/OAuthCallback': () => import('@/Views/OAuthCallback'),
};
