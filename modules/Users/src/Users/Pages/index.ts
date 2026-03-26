export const pages: Record<string, unknown> = {
  'Users/Account/TwoFactorAuthentication': () => import('./Account/TwoFactorAuthentication'),
  'Users/Account/EnableAuthenticator': () => import('./Account/EnableAuthenticator'),
  'Users/Account/Disable2fa': () => import('./Account/Disable2fa'),
  'Users/Account/ResetAuthenticator': () => import('./Account/ResetAuthenticator'),
  'Users/Account/GenerateRecoveryCodes': () => import('./Account/GenerateRecoveryCodes'),
  'Users/Account/ShowRecoveryCodes': () => import('./Account/ShowRecoveryCodes'),
};
