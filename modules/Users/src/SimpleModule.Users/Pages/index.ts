export const pages: Record<string, unknown> = {
  // Auth pages
  'Users/Account/Login': () => import('@/Views/Account/Login'),
  'Users/Account/Register': () => import('@/Views/Account/Register'),
  'Users/Account/Logout': () => import('@/Views/Account/Logout'),
  'Users/Account/RegisterConfirmation': () => import('@/Views/Account/RegisterConfirmation'),
  'Users/Account/ForgotPassword': () => import('@/Views/Account/ForgotPassword'),
  'Users/Account/ForgotPasswordConfirmation': () =>
    import('@/Views/Account/ForgotPasswordConfirmation'),
  'Users/Account/ResetPassword': () => import('@/Views/Account/ResetPassword'),
  'Users/Account/ResetPasswordConfirmation': () =>
    import('@/Views/Account/ResetPasswordConfirmation'),
  'Users/Account/ConfirmEmail': () => import('@/Views/Account/ConfirmEmail'),
  'Users/Account/ConfirmEmailChange': () => import('@/Views/Account/ConfirmEmailChange'),
  'Users/Account/ResendEmailConfirmation': () => import('@/Views/Account/ResendEmailConfirmation'),
  'Users/Account/LoginWith2fa': () => import('@/Views/Account/LoginWith2fa'),
  'Users/Account/LoginWithRecoveryCode': () => import('@/Views/Account/LoginWithRecoveryCode'),
  'Users/Account/Lockout': () => import('@/Views/Account/Lockout'),
  'Users/Account/AccessDenied': () => import('@/Views/Account/AccessDenied'),
  'Users/Account/Error': () => import('@/Views/Account/Error'),
  'Users/Account/ExternalLogin': () => import('@/Views/Account/ExternalLogin'),
  // Manage pages
  'Users/Account/Manage/Index': () => import('@/Views/Account/Manage/ManageIndex'),
  'Users/Account/Manage/Email': () => import('@/Views/Account/Manage/Email'),
  'Users/Account/Manage/ChangePassword': () => import('@/Views/Account/Manage/ChangePassword'),
  'Users/Account/Manage/SetPassword': () => import('@/Views/Account/Manage/SetPassword'),
  'Users/Account/Manage/DeletePersonalData': () =>
    import('@/Views/Account/Manage/DeletePersonalData'),
  'Users/Account/Manage/PersonalData': () => import('@/Views/Account/Manage/PersonalData'),
  'Users/Account/Manage/ExternalLogins': () => import('@/Views/Account/Manage/ExternalLogins'),
  'Users/Account/TwoFactorAuthentication': () => import('@/Pages/Account/TwoFactorAuthentication'),
  'Users/Account/EnableAuthenticator': () => import('@/Pages/Account/EnableAuthenticator'),
  'Users/Account/Disable2fa': () => import('@/Pages/Account/Disable2fa'),
  'Users/Account/ResetAuthenticator': () => import('@/Pages/Account/ResetAuthenticator'),
  'Users/Account/GenerateRecoveryCodes': () => import('@/Pages/Account/GenerateRecoveryCodes'),
  'Users/Account/ShowRecoveryCodes': () => import('@/Pages/Account/ShowRecoveryCodes'),
};
